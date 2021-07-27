using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Extensions;
using Accord.Services.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Accord.Services.Xp
{
    public sealed record CalculateParticipationRequest : IRequest;

    public class CalculateParticipation : AsyncRequestHandler<CalculateParticipationRequest>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMediator _mediator;

        public CalculateParticipation(IMediator mediator, IServiceScopeFactory serviceScopeFactory)
        {
            _mediator = mediator;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override Task Handle(CalculateParticipationRequest request, CancellationToken cancellationToken)
        {
            return Calculate();
        }

        private async Task Calculate()
        {
            const double MINIMUM_MINUTES_IN_VOICE = 5;

            var calculateFromDate = DateTime.Today.AddDays(-30);

            using (var userResetScope = _serviceScopeFactory.CreateScope())
            await using (var userResetContext = userResetScope.ServiceProvider.GetRequiredService<AccordContext>())
            {
                var usersToReset = await userResetContext
                    .Users
                    .Where(x => x.ParticipationPoints != 0 || x.ParticipationPercentile != 0 || x.ParticipationRank != 0)
                    .ToListAsync();

                foreach (var userToReset in usersToReset)
                {
                    userToReset.ParticipationPoints = 0;
                    userToReset.ParticipationPercentile = 0;
                    userToReset.ParticipationRank = 0;
                }

                await userResetContext.SaveChangesAsync();
            }

            var channelsExcludedFromXp = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromXp));

            using var queryScope = _serviceScopeFactory.CreateScope();
            await using var queryContext = queryScope.ServiceProvider.GetRequiredService<AccordContext>();

            var messagesQuery = await queryContext
                .UserMessages
                .AsNoTracking()
                .Where(x => x.SentDateTime >= calculateFromDate)
                .Where(x => !channelsExcludedFromXp.Contains(x.DiscordChannelId))
                .Select(x => new
                {
                    x.UserId,
                    x.SentDateTime
                })
                .ToListAsync();

            var voicesQuery = await queryContext
                .VoiceConnections
                .Where(x => x.EndDateTime != null && x.MinutesInVoiceChannel != null && x.MinutesInVoiceChannel > MINIMUM_MINUTES_IN_VOICE)
                .Where(x => x.EndDateTime >= calculateFromDate)
                .Where(x => !channelsExcludedFromXp.Contains(x.DiscordChannelId))
                .Select(x => new
                {
                    x.UserId,
                    x.StartDateTime,
                    x.MinutesInVoiceChannel,
                })
                .ToListAsync();

            // Group client side, because EF cannot translate this... Yet?
            var groupedMessages = messagesQuery.GroupBy(x => x.UserId).ToList();
            var groupedVoiceConnections = voicesQuery.GroupBy(x => x.UserId).ToList();

            var rankedMessengers = groupedMessages.OrderByDescending(x => x.Count()).ToList();
            var rankedVoiceUsers = groupedVoiceConnections.OrderByDescending(x => x.Sum(q => q.MinutesInVoiceChannel)).ToList();

            var userParticipation = groupedMessages
                .Select(x => x.Key)
                .Concat(groupedVoiceConnections.Select(d => d.Key))
                .Distinct()
                .Select(id => new UserParticipation(id))
                .ToList();

            foreach (var user in userParticipation)
            {
                var pointsForUser = 0;

                var userFirstSeen = await queryContext.Users
                    .Where(x => x.Id == user.DiscordUserId)
                    .Select(x => x.FirstSeenDateTime)
                    .FirstAsync();

                var messagesSentByUser = groupedMessages
                    .Where(x => x.Key == user.DiscordUserId)
                    .ToList();

                var voiceSessions = groupedVoiceConnections
                    .Where(x => x.Key == user.DiscordUserId)
                    .ToList();

                var dateToCalculateFromForUser = DateTimeHelper.Max(userFirstSeen, calculateFromDate);

                var activityDates = messagesSentByUser
                    .SelectMany(x => x.Select(q => q.SentDateTime.Date))
                    .Concat(voiceSessions.SelectMany(q => q.Select(c => c.StartDateTime.Date)))
                    .Distinct()
                    .ToList();

                foreach (var day in EachDay(dateToCalculateFromForUser.Date, DateTime.Today))
                {
                    var hasActivityInDay = activityDates.Any(date => date == day);
                    var hasActivityNextDay = activityDates.Any(date => date == day.AddDays(1));

                    if (hasActivityInDay && hasActivityNextDay)
                    {
                        pointsForUser += 10;
                    }
                }

                var rankedMessengerPosition = rankedMessengers.SingleOrDefault(x => x.Key == user.DiscordUserId);
                var rankedVoiceUserPosition = rankedVoiceUsers.SingleOrDefault(x => x.Key == user.DiscordUserId);

                if (rankedMessengerPosition is not null)
                {
                    pointsForUser += rankedMessengers.Count - rankedMessengers.IndexOf(rankedMessengerPosition);
                }

                if (rankedVoiceUserPosition is not null)
                {
                    pointsForUser += rankedVoiceUsers.Count - rankedVoiceUsers.IndexOf(rankedVoiceUserPosition);
                }

                user.Points = pointsForUser;
            }

            var orderedParticipation = userParticipation.OrderByDescending(x => x.Points).ToList();

            foreach (var user in orderedParticipation)
            {
                var rank = orderedParticipation.IndexOf(user) + 1;
                var percentile = (1 - (rank / (double)orderedParticipation.Count)) * 100;
                user.Percentile = percentile;
                user.Rank = rank;
            }

            var batches = orderedParticipation.Batch(150);

            foreach (var batch in batches)
            {
                using var userUpdateScope = _serviceScopeFactory.CreateScope();
                await using var userUpdateContext = userUpdateScope.ServiceProvider.GetRequiredService<AccordContext>();

                foreach (var user in batch)
                {
                    var userEntity = await userUpdateContext
                        .Users
                        .Where(x => x.Id == user.DiscordUserId)
                        .FirstAsync();

                    userEntity.ParticipationPercentile = user.Percentile;
                    userEntity.ParticipationPoints = user.Points;
                    userEntity.ParticipationRank = user.Rank;
                }

                await userUpdateContext.SaveChangesAsync();
            }

            static IEnumerable<DateTime> EachDay(DateTime from, DateTime until)
            {
                for (var day = from.Date; day.Date <= until.Date; day = day.AddDays(1))
                    yield return day;
            }
        }
    }

    internal class UserParticipation
    {
        public UserParticipation(ulong id)
        {
            DiscordUserId = id;
        }

        public ulong DiscordUserId { get; set; }
        public int Points { get; set; }
        public double Percentile { get; set; }
        public int Rank { get; set; }
    }
}

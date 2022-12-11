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

namespace Accord.Services.Participation;

public sealed record CalculateParticipationRequest : IRequest;

[AutoConstructor]
public partial class CalculateParticipation : AsyncRequestHandler<CalculateParticipationRequest>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMediator _mediator;

    protected override Task Handle(CalculateParticipationRequest request, CancellationToken cancellationToken) => Calculate();

    private async Task Calculate()
    {
        const double MINIMUM_MINUTES_IN_VOICE = 5;

        var calculateFromDate = DateTime.Today.AddDays(-30);

        using (var userResetScope = _serviceScopeFactory.CreateScope())
        await using (var userResetContext = userResetScope.ServiceProvider.GetRequiredService<AccordContext>())
        {
            await userResetContext.Users
                .Where(x => x.ParticipationRank != 0 || x.ParticipationPercentile != 0 || x.ParticipationPoints != 0)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(d => d.ParticipationPercentile, 0)
                    .SetProperty(d => d.ParticipationPoints, 0)
                    .SetProperty(d => d.ParticipationRank, 0));
        }

        var channelsExcludedFromXp = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromXp));

        using var queryScope = _serviceScopeFactory.CreateScope();
        await using var queryContext = queryScope.ServiceProvider.GetRequiredService<AccordContext>();

        var groupedMessages = await queryContext
            .UserMessages
            .Where(x => x.SentDateTime >= calculateFromDate)
            .Where(x => !channelsExcludedFromXp.Contains(x.DiscordChannelId))
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                x.Key,
                Messages = x.Select(d => d.SentDateTime).ToList(),
            })
            .ToListAsync();

        var groupedVoiceConnections = await queryContext
            .VoiceConnections
            .Where(x => x.EndDateTime != null && x.MinutesInVoiceChannel != null && x.MinutesInVoiceChannel > MINIMUM_MINUTES_IN_VOICE)
            .Where(x => x.EndDateTime >= calculateFromDate)
            .Where(x => !channelsExcludedFromXp.Contains(x.DiscordChannelId))
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                x.Key,
                Connections = x.Select(d => new
                {
                    d.StartDateTime,
                    d.MinutesInVoiceChannel,
                }).ToList()
            })
            .ToListAsync();

        var rankedMessengers = groupedMessages.OrderByDescending(x => x.Messages.Count).ToList();
        var rankedVoiceUsers = groupedVoiceConnections.OrderByDescending(x => x.Connections.Sum(q => q.MinutesInVoiceChannel)).ToList();

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
                .SelectMany(x => x.Messages.Select(q => q.Date))
                .Concat(voiceSessions.SelectMany(q => q.Connections.Select(c => c.StartDateTime.Date)))
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
            var percentile = (1 - rank / (double)orderedParticipation.Count) * 100;
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
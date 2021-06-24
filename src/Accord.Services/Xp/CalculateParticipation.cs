using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Xp
{
    public class CalculateParticipation
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public CalculateParticipation(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task Calculate()
        {
            var calculateFromDate = DateTimeOffset.Now;

            var channelsExcludedFromXp = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromXp));

            var messagesQuery = await _db
                .UserMessages
                .AsNoTracking()
                .Where(x => x.SentDateTime >= calculateFromDate)
                .Where(x => !channelsExcludedFromXp.Contains(x.DiscordChannelId))
                .GroupBy(x => x.UserId)
                .ToListAsync();

            var voicesQuery = await _db
                .VoiceConnections
                .AsNoTracking()
                .Where(x => x.EndDateTime != null && x.MinutesInVoiceChannel != null)
                .Where(x => x.EndDateTime >= calculateFromDate)
                .Where(x => !channelsExcludedFromXp.Contains(x.DiscordChannelId))
                .GroupBy(x => x.UserId)
                .ToListAsync();

            var rankedMessengers = messagesQuery.OrderByDescending(x => x.Count()).ToList();
            var rankedVoiceUsers = voicesQuery.OrderByDescending(x => x.Sum(q => q.MinutesInVoiceChannel)).ToList();

            var userParticipation = messagesQuery
                .Select(x => x.Key)
                .Concat(voicesQuery.Select(d => d.Key))
                .Distinct()
                .Select(id => new UserParticipation(id))
                .ToList();

            foreach (var user in userParticipation)
            {
                var pointsForUser = 0;

                var userStats = await _db.Users
                    .Where(x => x.Id == user.DiscordUserId)
                    .Select(x => new
                    {
                        x.FirstSeenDateTime,
                    }).SingleAsync();

                var messagesSentByUser = messagesQuery
                    .Where(x => x.Key == user.DiscordUserId)
                    .ToList();

                var voiceSessions = voicesQuery
                    .Where(x => x.Key == user.DiscordUserId)
                    .ToList();

                var dateToCalculateFromForUser = DateTimeHelper.Max(userStats.FirstSeenDateTime, calculateFromDate);

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
                        pointsForUser += 5;
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

                if (rankedMessengerPosition is not null
                    && rankedVoiceUserPosition is not null)
                {
                    pointsForUser += (rankedMessengers.Count - rankedMessengers.IndexOf(rankedMessengerPosition))
                        + (rankedVoiceUsers.Count - rankedVoiceUsers.IndexOf(rankedVoiceUserPosition));
                }

                user.Points = pointsForUser;
            }

            var orderedParticipation = userParticipation.OrderByDescending(x => x.Points).ToList();

            foreach (var user in orderedParticipation)
            {
                var rank = orderedParticipation.IndexOf(user) + 1;
                double percentile = (1 - (rank / orderedParticipation.Count)) * 100;
                user.Percentile = percentile;
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
    }
}

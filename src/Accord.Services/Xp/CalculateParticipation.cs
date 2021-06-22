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

            var rankedMessengers = messagesQuery.OrderByDescending(x => x.Count()).Take(10).ToList();
            var rankedTalkers = voicesQuery.OrderByDescending(x => x.Sum(q => q.MinutesInVoiceChannel)).Take(10).ToList();

            var allUserIds = messagesQuery
                .Select(x => x.Key)
                .Concat(voicesQuery.Select(d => d.Key))
                .Distinct()
                .Select(id => new UserParticipation(id));

            foreach (var user in allUserIds)
            {
                var userStats = await _db.Users
                    .Where(x => x.Id == user.DiscordUserId)
                    .Select(x => new
                    {
                        x.FirstSeenDateTime,
                    }).SingleAsync();

                var messagesSentByUser = messagesQuery
                    .Where(x => x.Key == user.DiscordUserId)
                    .ToList();

                var voiceSessions = messagesQuery
                    .Where(x => x.Key == user.DiscordUserId)
                    .ToList();

                var dateToCalculateFromForUser = DateTimeHelper.Max(userStats.FirstSeenDateTime, calculateFromDate);
                var daysInCalculation = (DateTimeOffset.Now - calculateFromDate).Days;

                var consistencyScale = messagesSentByUser.GroupBy(x => x.Select(q => q.SentDateTime.Date));

                // TODO Calculate participation
            }
        }
    }

    internal sealed record UserParticipation(ulong DiscordUserId, int Points = 0);
}

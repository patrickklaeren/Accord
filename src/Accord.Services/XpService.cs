using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class XpService
    {
        private readonly AccordContext _db;
        private readonly ChannelFlagService _channelFlagService;

        public XpService(AccordContext db, ChannelFlagService channelFlagService)
        {
            _db = db;
            _channelFlagService = channelFlagService;
        }

        public async Task<List<XpUser>> GetLeaderboard()
        {
            return await _db.Users
                .OrderByDescending(x => x.Xp)
                .ThenBy(x => x.LastSeenDateTime)
                .Take(10)
                .Select(x => new XpUser(x.Id, x.Xp))
                .ToListAsync();
        }

        public async Task CalculateXp(ulong discordUserId, ulong discordChannelId, DateTimeOffset messageSentDateTime, CancellationToken stoppingToken = default)
        {
            if (await _channelFlagService.IsChannelIgnoredFromXp(discordChannelId))
                return;

            var user = await _db.Users
                .SingleOrDefaultAsync(x => x.Id == discordUserId, cancellationToken: stoppingToken);

            if (user is null)
            {
                user = new User
                {
                    Id = discordUserId,
                    FirstSeenDateTime = DateTimeOffset.Now,
                };

                _db.Add(user);
            }

            if (user.LastSeenDateTime.AddSeconds(10) > messageSentDateTime)
            {
                user.Xp += 5;
            }

            user.LastSeenDateTime = messageSentDateTime;

            await _db.SaveChangesAsync(stoppingToken);
        }
    }

    public record XpUser(ulong DiscordUserId, float Xp);
}

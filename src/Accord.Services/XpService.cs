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

        public async Task<Leaderboard> GetLeaderboard()
        {
            var messageUsers = await _db.Users
                .OrderByDescending(x => x.Xp)
                .ThenBy(x => x.LastSeenDateTime)
                .Take(10)
                .Select(x => new MessageUser(x.Id, x.Xp))
                .ToListAsync();

            var voiceUsers = await _db.VoiceConnections
                .Where(x => x.MinutesInVoiceChannel != null)
                .GroupBy(x => x.UserId)
                .OrderByDescending(x => x.Sum(q => q.MinutesInVoiceChannel!.Value))
                .Take(10)
                .Select(x => new VoiceUser(x.Key, x.Sum(q => q.MinutesInVoiceChannel!.Value)))
                .ToListAsync();

            return new Leaderboard(messageUsers, voiceUsers);
        }

        public async Task AddXpForMessage(ulong discordUserId, ulong discordChannelId, DateTimeOffset messageSentDateTime, CancellationToken stoppingToken = default)
        {
            if (await _channelFlagService.IsChannelIgnoredFromXp(discordChannelId))
                return;

            var user = await _db.Users.SingleAsync(x => x.Id == discordUserId, cancellationToken: stoppingToken);

            if (user.LastSeenDateTime.AddSeconds(10) > messageSentDateTime)
            {
                user.Xp += 5;
            }

            user.LastSeenDateTime = messageSentDateTime;

            await _db.SaveChangesAsync(stoppingToken);
        }
    }

    public record MessageUser(ulong DiscordUserId, float Xp);
    public record VoiceUser(ulong DiscordUserId, double MinutesInVoiceChannel);

    public record Leaderboard(List<MessageUser> MessageUsers, List<VoiceUser> VoiceUsers);
}

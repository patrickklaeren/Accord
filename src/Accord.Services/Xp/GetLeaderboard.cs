using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Xp
{
    public sealed record GetLeaderboardRequest() : IRequest<Leaderboard>;

    public sealed record Leaderboard(List<MessageUser> MessageUsers, List<VoiceUser> VoiceUsers);
    public record MessageUser(ulong DiscordUserId, float Xp);
    public record VoiceUser(ulong DiscordUserId, double MinutesInVoiceChannel);

    public class GetLeaderboardHandler : IRequestHandler<GetLeaderboardRequest, Leaderboard>
    {
        private readonly AccordContext _db;

        public GetLeaderboardHandler(AccordContext db)
        {
            _db = db;
        }

        public async Task<Leaderboard> Handle(GetLeaderboardRequest request, CancellationToken cancellationToken)
        {
            var messageUsers = await _db.Users
                .OrderByDescending(x => x.Xp)
                .ThenBy(x => x.LastSeenDateTime)
                .Take(10)
                .Select(x => new MessageUser(x.Id, x.Xp))
                .ToListAsync(cancellationToken: cancellationToken);

            var voiceUsers = await _db.VoiceConnections
                .Where(x => x.MinutesInVoiceChannel != null)
                .GroupBy(x => x.UserId)
                .OrderByDescending(x => x.Sum(q => q.MinutesInVoiceChannel!.Value))
                .Take(10)
                .Select(x => new VoiceUser(x.Key, x.Sum(q => q.MinutesInVoiceChannel!.Value)))
                .ToListAsync(cancellationToken: cancellationToken);

            return new Leaderboard(messageUsers, voiceUsers);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Participation;

public sealed record GetLeaderboardRequest : IRequest<Leaderboard>;

public sealed record Leaderboard(List<MessageUser> MessageUsers);
public record MessageUser(ulong DiscordUserId, float ParticipationPoints);

public class GetLeaderboardHandler : IRequestHandler<GetLeaderboardRequest, Leaderboard>
{
    private readonly AccordContext _db;

    public GetLeaderboardHandler(AccordContext db)
    {
        _db = db;
    }

    public async Task<Leaderboard> Handle(GetLeaderboardRequest request, CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .OrderByDescending(x => x.ParticipationPoints)
            .ThenBy(x => x.LastSeenDateTime)
            .Take(10)
            .Select(x => new MessageUser(x.Id, x.ParticipationPoints))
            .ToListAsync(cancellationToken: cancellationToken);

        return new Leaderboard(users);
    }
}
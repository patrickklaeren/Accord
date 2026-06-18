using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record GetUserNameDiffRequest(ulong DiscordUserId, string DiscordUsername, string? DiscordNickname) : IRequest<UserNameDiffResponse>;
public sealed record UserNameDiffResponse(bool HasDiff, List<string> Messages);

public class GetUserNameDiffHandler(AccordContext db) : IRequestHandler<GetUserNameDiffRequest, UserNameDiffResponse>
{
    public async Task<UserNameDiffResponse> Handle(GetUserNameDiffRequest request, CancellationToken cancellationToken)
    {
        var diffMessages = new List<string>();

        var user = await db.Users
            .Where(x => x.Id == request.DiscordUserId)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.Nickname
            }).SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (user is null)
            return new UserNameDiffResponse(false, diffMessages);

        if (!string.IsNullOrWhiteSpace(user.Username) && user.Username != request.DiscordUsername)
        {
            diffMessages.Add($"Changed handle from {user.Username} to {request.DiscordUsername}");
        }

        if (user.Nickname != request.DiscordNickname)
        {
            if (string.IsNullOrWhiteSpace(user.Nickname))
            {
                diffMessages.Add($"Set nickname to {request.DiscordNickname}");
            }
            else
            {
                diffMessages.Add($"Changed nickname from {user.Nickname} to {request.DiscordNickname}");
            }
        }

        return new UserNameDiffResponse(diffMessages.Any(), diffMessages);
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record GetUserNameDiffRequest(ulong DiscordUserId, string DiscordUsername, string DiscordDiscriminator, string? DiscordNickname) : IRequest<UserNameDiffResponse>;
public sealed record UserNameDiffResponse(bool HasDiff, List<string> Messages);

[AutoConstructor]
public partial class GetUserNameDiffHandler : IRequestHandler<GetUserNameDiffRequest, UserNameDiffResponse>
{
    private readonly AccordContext _db;

    public async Task<UserNameDiffResponse> Handle(GetUserNameDiffRequest request, CancellationToken cancellationToken)
    {
        var diffMessages = new List<string>();

        var user = await _db.Users
            .Where(x => x.Id == request.DiscordUserId)
            .Select(x => new
            {
                x.Id,
                x.UsernameWithDiscriminator,
                x.Nickname
            }).SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (user is null)
            return new UserNameDiffResponse(false, diffMessages);

        var handle = DiscordHandleHelper.BuildHandle(request.DiscordUsername, request.DiscordDiscriminator);

        if (!string.IsNullOrWhiteSpace(user.UsernameWithDiscriminator) && user.UsernameWithDiscriminator != handle)
        {
            diffMessages.Add($"Changed handle from {user.UsernameWithDiscriminator} to {handle}");
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
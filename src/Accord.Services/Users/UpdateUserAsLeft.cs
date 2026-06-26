using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record UpdateUserAsLeftRequest(
    ulong DiscordUserId,
    DateTimeOffset LeftAtDateTime) : IRequest;

internal class UpdateUserAsLeftHandler(UserService userService) : IRequestHandler<UpdateUserAsLeftRequest>
{
    public async Task Handle(UpdateUserAsLeftRequest request, CancellationToken cancellationToken)
    {
        await userService.UpdateUserAsLeft(request.DiscordUserId,
            request.LeftAtDateTime, 
            cancellationToken);
    }
}
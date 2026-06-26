using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record UpdateUserRequest(
    ulong DiscordUserId,
    string DiscordUsername,
    string? DiscordNickname,
    DateTimeOffset? TimedOutUntil,
    string? DiscordAvatarUrl,
    DateTimeOffset? JoinedDateTime) : IRequest;

internal class UpdateUserHandler(UserService userService) : IRequestHandler<UpdateUserRequest>
{
    public async Task Handle(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await userService.UpdateUser(request.DiscordUserId, 
            request.DiscordUsername, 
            request.DiscordNickname,  
            request.JoinedDateTime, 
            request.TimedOutUntil, 
            cancellationToken);
    }
}
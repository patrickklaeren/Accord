using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.Users;

public sealed record AddUserRequest(
    ulong DiscordUserId,
    string DiscordUsername,
    string? DiscordAvatarUrl,
    string? DiscordNickname,
    DateTimeOffset JoinedDateTime) : IRequest;

internal class AddUserHandler(UserService userService) : IRequestHandler<AddUserRequest>
{
    public async Task Handle(AddUserRequest request, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(request.DiscordUserId, cancellationToken);

        if (userExists)
            return;
        
        var user = new User
        {
            Id = request.DiscordUserId,
            FirstSeenDateTime = request.JoinedDateTime,
            JoinedGuildDateTime = request.JoinedDateTime,
            Username = request.DiscordUsername,
            Nickname = request.DiscordNickname,
        };
        
        await userService.AddUser(user, cancellationToken);
    }
}
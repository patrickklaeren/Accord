using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Moderation;
using MediatR;

namespace Accord.Services.Users;

public sealed record AddUserRequest(ulong DiscordGuildId,
    ulong DiscordUserId,
    string DiscordUsername,
    string DiscordDiscriminator,
    string? DiscordAvatarUrl,
    string? DiscordNickname,
    DateTimeOffset JoinedDateTime) : IRequest;

[AutoConstructor]
public partial class AddUserHandler : AsyncRequestHandler<AddUserRequest>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    protected override async Task Handle(AddUserRequest request, CancellationToken cancellationToken)
    {
        var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

        if (userExists)
            return;

        var user = new User
        {
            Id = request.DiscordUserId,
            FirstSeenDateTime = request.JoinedDateTime,
            JoinedGuildDateTime = request.JoinedDateTime,
            UsernameWithDiscriminator = $"{request.DiscordUsername}#{request.DiscordDiscriminator}",
            Nickname = request.DiscordNickname,
        };

        _db.Add(user);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateUserExistsRequest(request.DiscordUserId), cancellationToken);
    }
}
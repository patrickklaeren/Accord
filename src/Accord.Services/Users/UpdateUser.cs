using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record UpdateUserRequest(ulong DiscordGuildId,
    ulong DiscordUserId,
    string DiscordUsername,
    string DiscordDiscriminator,
    string? DiscordNickname,
    DateTimeOffset? TimedOutUntil,
    string? DiscordAvatarUrl,
    DateTimeOffset? JoinedDateTime) : IRequest;

[AutoConstructor]
public partial class UpdateUserHandler : IRequestHandler<UpdateUserRequest>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task Handle(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .SingleOrDefaultAsync(x => x.Id == request.DiscordUserId, cancellationToken);

        if (user is null)
            return;

        var usernameWithDiscriminator = $"{request.DiscordUsername}#{request.DiscordDiscriminator}";

        if (user.UsernameWithDiscriminator != usernameWithDiscriminator
            || user.Nickname != request.DiscordNickname
            || user.TimedOutUntil != request.TimedOutUntil)
        {
            _appCache.Remove(GetUserHandler.BuildGetUserCacheKey(request.DiscordUserId));
            _appCache.Remove(HasTimeOutChangedHandler.BuildCacheKey(request.DiscordUserId));
        }

        user.JoinedGuildDateTime = request.JoinedDateTime;
        user.UsernameWithDiscriminator = usernameWithDiscriminator;
        user.Nickname = request.DiscordNickname;
        user.TimedOutUntil = request.TimedOutUntil;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
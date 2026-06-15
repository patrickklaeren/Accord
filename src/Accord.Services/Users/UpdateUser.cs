using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record UpdateUserRequest(
    ulong DiscordUserId,
    string DiscordUsername,
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

        if (user.Username != request.DiscordUsername
            || user.Nickname != request.DiscordNickname
            || user.TimedOutUntil != request.TimedOutUntil)
        {
            _appCache.Remove(GetUserHandler.BuildGetUserCacheKey(request.DiscordUserId));
            _appCache.Remove(HasTimeOutChangedHandler.BuildCacheKey(request.DiscordUserId));
        }

        user.JoinedGuildDateTime = request.JoinedDateTime;
        user.Username = request.DiscordUsername;
        user.Nickname = request.DiscordNickname;
        user.TimedOutUntil = request.TimedOutUntil;
        user.LastSeenDateTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
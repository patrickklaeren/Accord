using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

[RegisterScoped]
public class UserService(AccordContext db, IAppCache appCache)
{
    public async Task<bool> UserExists(ulong discordUserId, CancellationToken cancellationToken)
    {
        return await appCache
            .GetOrAddAsync(BuildUserExistsCacheKey(discordUserId),
                () => db.Users.AnyAsync(x => x.Id == discordUserId, cancellationToken),
                DateTimeOffset.UtcNow.AddDays(30));
    }

    public async Task<UserDto> GetUser(ulong discordUserId, CancellationToken cancellationToken)
    {
        return await appCache.GetOrAddAsync(BuildGetUserCacheKey(discordUserId), GetData);

        async Task<UserDto> GetData()
        {
            return await db.Users
                .Where(x => x.Id == discordUserId)
                .Select(x => new UserDto(x.Id,
                    x.Username,
                    x.Nickname,
                    x.JoinedGuildDateTime,
                    x.FirstSeenDateTime,
                    x.FirstSeenDateTime,
                    x.ParticipationRank,
                    x.ParticipationPoints,
                    x.ParticipationPercentile))
                .SingleAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task AddUser(ulong discordUserId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = discordUserId,
            FirstSeenDateTime = now,
            LastSeenDateTime = now,
        };

        await AddUser(user, cancellationToken);
    }

    public async Task AddUser(User user, CancellationToken cancellationToken)
    {
        db.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        InvalidateCache(user.Id);
    }

    public async Task UpdateUser(ulong discordUserId,
        string username,
        string? nickname,
        DateTimeOffset? joinedGuildDateTime,
        DateTimeOffset? timedOutUntil,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .SingleOrDefaultAsync(x => x.Id == discordUserId, cancellationToken);

        if (user is null)
            return;

        user.JoinedGuildDateTime = joinedGuildDateTime;
        user.Username = username;
        user.Nickname = nickname;
        user.TimedOutUntil = timedOutUntil;
        user.LastSeenDateTime = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        InvalidateCache(discordUserId);
    }

    private void InvalidateCache(ulong discordUserId)
    {
        appCache.Remove(BuildUserExistsCacheKey(discordUserId));
        appCache.Remove(BuildGetUserCacheKey(discordUserId));
    }

    private static string BuildGetUserCacheKey(ulong discordUserId)
    {
        return $"{nameof(GetUserHandler)}/{nameof(GetUser)}/{discordUserId}";
    }

    private static string BuildUserExistsCacheKey(ulong discordUserId)
    {
        return $"{nameof(UserExists)}/{nameof(UserExists)}/{discordUserId}";
    }
}

public sealed record UserDto(
    ulong Id,
    string? Username,
    string? Nickname,
    DateTimeOffset? JoinedGuildDateTime,
    DateTimeOffset FirstSeenDateTime,
    DateTimeOffset? TimedOutUntilDateTime,
    int ParticipationRank,
    int ParticipationPoints,
    double ParticipationPercentile);
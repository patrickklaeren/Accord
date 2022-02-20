using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record UserExistsRequest(ulong DiscordUserId) : IRequest<bool>;

public sealed record InvalidateUserExistsRequest(ulong DiscordUserId) : IRequest;

public class DoesUserExistHandler : RequestHandler<InvalidateUserExistsRequest>, IRequestHandler<UserExistsRequest, bool>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public DoesUserExistHandler(AccordContext db, IAppCache appCache)
    {
        _db = db;
        _appCache = appCache;
    }

    public async Task<bool> Handle(UserExistsRequest request, CancellationToken cancellationToken)
    {
        return await _appCache
            .GetOrAddAsync(GetCacheKey(request.DiscordUserId),
                () => _db.Users.AnyAsync(x => x.Id == request.DiscordUserId, cancellationToken),
                DateTimeOffset.Now.AddDays(30));
    }

    protected override void Handle(InvalidateUserExistsRequest request)
    {
        _appCache.Remove(GetCacheKey(request.DiscordUserId));
    }

    private static string GetCacheKey(ulong discordUserId)
    {
        return $"{nameof(EnsureUserExistsHandler)}/{nameof(UserExistsRequest)}/{discordUserId}";
    }
}
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

[AutoConstructor]
public partial class DoesUserExistHandler : IRequestHandler<InvalidateUserExistsRequest>, IRequestHandler<UserExistsRequest, bool>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<bool> Handle(UserExistsRequest request, CancellationToken cancellationToken)
    {
        return await _appCache
            .GetOrAddAsync(GetCacheKey(request.DiscordUserId),
                () => _db.Users.AnyAsync(x => x.Id == request.DiscordUserId, cancellationToken),
                DateTimeOffset.Now.AddDays(30));
    }

    private static string GetCacheKey(ulong discordUserId)
    {
        return $"{nameof(EnsureUserExistsHandler)}/{nameof(UserExistsRequest)}/{discordUserId}";
    }

    public Task Handle(InvalidateUserExistsRequest request, CancellationToken cancellationToken)
    {
        _appCache.Remove(GetCacheKey(request.DiscordUserId));
        return Task.CompletedTask;
    }
}
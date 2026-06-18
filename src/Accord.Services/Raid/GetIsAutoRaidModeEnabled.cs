using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Raid;

public sealed record GetIsAutoRaidModeEnabledRequest : IRequest<bool>;
public sealed record InvalidateGetIsAutoRaidModeEnabledRequest : IRequest;

public class GetIsAutoRaidModeEnabledHandler(AccordContext db, IAppCache appCache) : IRequestHandler<InvalidateGetIsAutoRaidModeEnabledRequest>, IRequestHandler<GetIsAutoRaidModeEnabledRequest, bool>
{

    public async Task<bool> Handle(GetIsAutoRaidModeEnabledRequest request, CancellationToken cancellationToken)
    {
        return await appCache.GetOrAddAsync(BuildGetIsAutoRaidModeEnabledCacheKey(),
            GetIsAutoRaidModeEnabled,
            DateTimeOffset.UtcNow.AddDays(30));
    }

    private static string BuildGetIsAutoRaidModeEnabledCacheKey()
    {
        return $"{nameof(GetIsAutoRaidModeEnabledHandler)}/{nameof(GetIsAutoRaidModeEnabled)}";
    }

    private async Task<bool> GetIsAutoRaidModeEnabled()
    {
        var value = await db.RunOptions
            .Where(x => x.Type == RunOptionType.AutoRaidModeEnabled)
            .Select(x => x.Value)
            .SingleAsync();

        return bool.Parse(value);
    }

    public Task Handle(InvalidateGetIsAutoRaidModeEnabledRequest request, CancellationToken cancellationToken)
    {
        appCache.Remove(BuildGetIsAutoRaidModeEnabledCacheKey());
        return Task.CompletedTask;
    }
}
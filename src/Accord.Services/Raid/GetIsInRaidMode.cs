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

public sealed record GetIsInRaidModeRequest : IRequest<bool>;
public sealed record InvalidateGetIsInRaidModeRequest : IRequest;

public class GetIsInRaidModeHandler(AccordContext db, IAppCache appCache) : IRequestHandler<InvalidateGetIsInRaidModeRequest>, IRequestHandler<GetIsInRaidModeRequest, bool>
{

    public async Task<bool> Handle(GetIsInRaidModeRequest request, CancellationToken cancellationToken)
    {
        return await appCache.GetOrAddAsync(BuildGetIsInRaidModeCacheKey(),
            GetIsInRaidMode,
            DateTimeOffset.UtcNow.AddDays(30));
    }

    private static string BuildGetIsInRaidModeCacheKey()
    {
        return $"{nameof(GetIsInRaidModeHandler)}/{nameof(GetIsInRaidMode)}";
    }

    private async Task<bool> GetIsInRaidMode()
    {
        var value = await db.RunOptions
            .Where(x => x.Type == RunOptionType.RaidModeEnabled)
            .Select(x => x.Value)
            .SingleAsync();

        return bool.Parse(value);
    }

    public Task Handle(InvalidateGetIsInRaidModeRequest request, CancellationToken cancellationToken)
    {
        appCache.Remove(BuildGetIsInRaidModeCacheKey());
        return Task.CompletedTask;
    }
}
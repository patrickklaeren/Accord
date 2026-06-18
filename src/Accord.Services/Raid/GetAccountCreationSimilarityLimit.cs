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

public sealed record GetAccountCreationSimilarityLimitRequest : IRequest<int>;
public sealed record InvalidateGetAccountCreationSimilarityLimitRequest : IRequest;

public class GetAccountCreationSimilarityLimitHandler(AccordContext db, IAppCache appCache) : IRequestHandler<InvalidateGetAccountCreationSimilarityLimitRequest>, IRequestHandler<GetAccountCreationSimilarityLimitRequest, int>
{

    public async Task<int> Handle(GetAccountCreationSimilarityLimitRequest request, CancellationToken cancellationToken)
    {
        return await appCache.GetOrAddAsync(BuildGetLimitCacheKey(),
            GetLimit,
            DateTimeOffset.UtcNow.AddDays(30));
    }

    private static string BuildGetLimitCacheKey()
    {
        return $"{nameof(GetAccountCreationSimilarityLimitHandler)}/{nameof(GetLimit)}";
    }

    private async Task<int> GetLimit()
    {
        var value = await db.RunOptions
            .Where(x => x.Type == RunOptionType.AccountCreationSimilarityJoinsToTriggerRaidMode)
            .Select(x => x.Value)
            .SingleAsync();

        return int.Parse(value);
    }

    public Task Handle(InvalidateGetAccountCreationSimilarityLimitRequest request, CancellationToken cancellationToken)
    {
        appCache.Remove(BuildGetLimitCacheKey());
        return Task.CompletedTask;
    }
}
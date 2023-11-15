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

[AutoConstructor]
public partial class GetAccountCreationSimilarityLimitHandler : IRequestHandler<InvalidateGetAccountCreationSimilarityLimitRequest>, IRequestHandler<GetAccountCreationSimilarityLimitRequest, int>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<int> Handle(GetAccountCreationSimilarityLimitRequest request, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildGetLimitCacheKey(),
            GetLimit,
            DateTimeOffset.Now.AddDays(30));
    }

    private static string BuildGetLimitCacheKey()
    {
        return $"{nameof(GetAccountCreationSimilarityLimitHandler)}/{nameof(GetLimit)}";
    }

    private async Task<int> GetLimit()
    {
        var value = await _db.RunOptions
            .Where(x => x.Type == RunOptionType.AccountCreationSimilarityJoinsToTriggerRaidMode)
            .Select(x => x.Value)
            .SingleAsync();

        return int.Parse(value);
    }

    public Task Handle(InvalidateGetAccountCreationSimilarityLimitRequest request, CancellationToken cancellationToken)
    {
        _appCache.Remove(BuildGetLimitCacheKey());
        return Task.CompletedTask;
    }
}
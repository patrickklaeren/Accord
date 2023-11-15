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

public sealed record GetJoinLimitPerMinuteRequest : IRequest<int>; 
public sealed record InvalidateGetJoinLimitPerMinuteRequest : IRequest;

[AutoConstructor]
public partial class GetJoinLimitPerMinuteHandler : IRequestHandler<InvalidateGetJoinLimitPerMinuteRequest>, IRequestHandler<GetJoinLimitPerMinuteRequest, int>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<int> Handle(GetJoinLimitPerMinuteRequest request, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildGetLimitPerOneMinuteCacheKey(),
            GetLimitPerOneMinute,
            DateTimeOffset.Now.AddDays(30));
    }

    private static string BuildGetLimitPerOneMinuteCacheKey()
    {
        return $"{nameof(GetJoinLimitPerMinuteHandler)}/{nameof(GetLimitPerOneMinute)}";
    }

    private async Task<int> GetLimitPerOneMinute()
    {
        var value = await _db.RunOptions
            .Where(x => x.Type == RunOptionType.SequentialJoinsToTriggerRaidMode)
            .Select(x => x.Value)
            .SingleAsync();

        return int.Parse(value);
    }

    public Task Handle(InvalidateGetJoinLimitPerMinuteRequest request, CancellationToken cancellationToken)
    {
        _appCache.Remove(BuildGetLimitPerOneMinuteCacheKey());
        return Task.CompletedTask;
    }
}
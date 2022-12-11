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
public partial class GetJoinLimitPerMinuteHandler : RequestHandler<InvalidateGetJoinLimitPerMinuteRequest>, IRequestHandler<GetJoinLimitPerMinuteRequest, int>
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

    protected override void Handle(InvalidateGetJoinLimitPerMinuteRequest request)
    {
        _appCache.Remove(BuildGetLimitPerOneMinuteCacheKey());
    }
}
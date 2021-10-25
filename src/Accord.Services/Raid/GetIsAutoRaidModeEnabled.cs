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

public class GetIsAutoRaidModeEnabledHandler : RequestHandler<InvalidateGetIsAutoRaidModeEnabledRequest>, IRequestHandler<GetIsAutoRaidModeEnabledRequest, bool>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public GetIsAutoRaidModeEnabledHandler(AccordContext db, IAppCache appCache)
    {
        _db = db;
        _appCache = appCache;
    }

    public async Task<bool> Handle(GetIsAutoRaidModeEnabledRequest request, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildGetIsAutoRaidModeEnabledCacheKey(),
            GetIsAutoRaidModeEnabled,
            DateTimeOffset.Now.AddDays(30));
    }

    private static string BuildGetIsAutoRaidModeEnabledCacheKey()
    {
        return $"{nameof(GetIsAutoRaidModeEnabledHandler)}/{nameof(GetIsAutoRaidModeEnabled)}";
    }

    private async Task<bool> GetIsAutoRaidModeEnabled()
    {
        var value = await _db.RunOptions
            .Where(x => x.Type == RunOptionType.AutoRaidModeEnabled)
            .Select(x => x.Value)
            .SingleAsync();

        return bool.Parse(value);
    }

    protected override void Handle(InvalidateGetIsAutoRaidModeEnabledRequest request)
    {
        _appCache.Remove(BuildGetIsAutoRaidModeEnabledCacheKey());
    }
}
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

[AutoConstructor]
public partial class GetIsInRaidModeHandler : IRequestHandler<InvalidateGetIsInRaidModeRequest>, IRequestHandler<GetIsInRaidModeRequest, bool>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<bool> Handle(GetIsInRaidModeRequest request, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildGetIsInRaidModeCacheKey(),
            GetIsInRaidMode,
            DateTimeOffset.Now.AddDays(30));
    }

    private static string BuildGetIsInRaidModeCacheKey()
    {
        return $"{nameof(GetIsInRaidModeHandler)}/{nameof(GetIsInRaidMode)}";
    }

    private async Task<bool> GetIsInRaidMode()
    {
        var value = await _db.RunOptions
            .Where(x => x.Type == RunOptionType.RaidModeEnabled)
            .Select(x => x.Value)
            .SingleAsync();

        return bool.Parse(value);
    }

    public Task Handle(InvalidateGetIsInRaidModeRequest request, CancellationToken cancellationToken)
    {
        _appCache.Remove(BuildGetIsInRaidModeCacheKey());
        return Task.CompletedTask;
    }
}
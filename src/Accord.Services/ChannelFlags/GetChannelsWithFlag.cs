using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags;

public sealed record GetChannelsWithFlagRequest(ChannelFlagType Flag) : IRequest<List<ulong>>;

public sealed record InvalidateGetChannelsWithFlagRequest(ChannelFlagType Flag) : IRequest;

[AutoConstructor]
public partial class GetChannelsWithFlagHandler : RequestHandler<InvalidateGetChannelsWithFlagRequest>, IRequestHandler<GetChannelsWithFlagRequest, List<ulong>>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<List<ulong>> Handle(GetChannelsWithFlagRequest request, CancellationToken cancellationToken)
    {
        return await _appCache.GetOrAddAsync(BuildGetChannelsWithFlagKey(request.Flag),
            () => GetChannelsWithFlag(request.Flag),
            DateTimeOffset.Now.AddDays(30));
    }

    protected override void Handle(InvalidateGetChannelsWithFlagRequest request)
    {
        _appCache.Remove(BuildGetChannelsWithFlagKey(request.Flag));
    }

    private async Task<List<ulong>> GetChannelsWithFlag(ChannelFlagType type)
    {
        return await _db.ChannelFlags
            .Where(x => x.Type == type)
            .Select(x => x.DiscordChannelId)
            .ToListAsync();
    }

    private static string BuildGetChannelsWithFlagKey(ChannelFlagType type)
    {
        return $"{nameof(GetChannelsWithFlagHandler)}/{nameof(GetChannelsWithFlag)}/{type}";
    }
}
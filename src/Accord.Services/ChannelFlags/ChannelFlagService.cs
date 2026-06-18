using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags;

[RegisterScoped]
public class ChannelFlagService(AccordContext db, IAppCache appCache)
{
    public async Task<List<ulong>> GetChannelIdsWithFlag(ChannelFlagType flag, CancellationToken cancellationToken)
    {
        return await appCache.GetOrAddAsync(BuildGetChannelsWithFlagKey(flag),
            () => GetChannelsWithFlag(flag),
            DateTimeOffset.UtcNow.AddDays(30));
    }

    public async Task<ServiceResponse> AddChannelFlag(ulong discordChannelId, ChannelFlagType flag, CancellationToken cancellationToken)
    {
        var hasExistingFlag = await db.ChannelFlags
            .Where(x => x.DiscordChannelId == discordChannelId)
            .AnyAsync(x => x.Type == flag, cancellationToken: cancellationToken);

        if (hasExistingFlag)
        {
            return ServiceResponse.Ok();
        }

        var entity = new ChannelFlag
        {
            DiscordChannelId = discordChannelId,
            Type = flag,
        };

        db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        
        InvalidateCache(flag);
        
        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> DeleteChannelFlag(ulong discordChannelId, ChannelFlagType requestFlag)
    {
        await db.ChannelFlags
            .Where(x => x.DiscordChannelId == discordChannelId)
            .Where(x => x.Type == requestFlag)
            .ExecuteDeleteAsync();
        
        return ServiceResponse.Ok();
    }

    private async Task<List<ulong>> GetChannelsWithFlag(ChannelFlagType type)
    {
        return await db.ChannelFlags
            .Where(x => x.Type == type)
            .Select(x => x.DiscordChannelId)
            .ToListAsync();
    }

    private void InvalidateCache(ChannelFlagType flag)
    {
        appCache.Remove(BuildGetChannelsWithFlagKey(flag));
    }

    private static string BuildGetChannelsWithFlagKey(ChannelFlagType type)
    {
        return $"{nameof(GetChannelsWithFlagHandler)}/{nameof(GetChannelsWithFlag)}/{type}";
    }
}
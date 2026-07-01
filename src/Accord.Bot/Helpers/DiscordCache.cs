using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Accord.Services;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Helpers;

[RegisterSingleton]
public class DiscordCache(IDiscordRestGuildAPI guildApi,
    IDiscordRestUserAPI userApi,
    IAppCache appCache, 
    DiscordConfiguration discordConfiguration)
{
    private readonly MemoryCacheEntryOptions _neverExpireCacheEntryOptions = new()
    {
        Priority = CacheItemPriority.NeverRemove
    };

    public async Task<Snowflake> GetSelfSnowflake()
    {
        return await appCache.GetOrAddAsync($"{nameof(DiscordCache)}/SelfSnowflake", Get, _neverExpireCacheEntryOptions);

        async Task<Snowflake> Get()
        {
            var currentUser = await userApi.GetCurrentUserAsync();
            return currentUser.Entity.ID;
        }
    }

    public IGuildMember GetGuildSelfMember() => appCache.Get<IGuildMember>($"{nameof(DiscordCache)}/SelfMember");
    public void SetGuildSelfMember(IGuildMember guildMember) => appCache.Add($"{nameof(DiscordCache)}/SelfMember", guildMember, _neverExpireCacheEntryOptions);

    private string GetGuildRolesCacheKey() => nameof(DiscordCache) + "/Roles";

    public async Task<IReadOnlyList<IRole>> GetGuildRoles()
    {
        return await appCache.GetOrAddAsync(GetGuildRolesCacheKey(), GetGuildRolesData, _neverExpireCacheEntryOptions);

        async Task<IReadOnlyList<IRole>> GetGuildRolesData()
        {
            var guild = await guildApi.GetGuildAsync(new Snowflake(discordConfiguration.GuildId), true);
            return !guild.IsSuccess ? [] : guild.Entity.Roles;
        }
    }

    public void InvalidateGuildRoles() => appCache.Remove(GetGuildRolesCacheKey());
    private string GetGuildChannelsCacheKey() => nameof(DiscordCache) + "/Channels";

    public async Task<IReadOnlyList<IChannel>> GetGuildChannels()
    {
        return await appCache.GetOrAddAsync(GetGuildChannelsCacheKey(), GetGuildChannelsData, _neverExpireCacheEntryOptions);

        async Task<IReadOnlyList<IChannel>> GetGuildChannelsData()
        {
            var channels = await guildApi.GetGuildChannelsAsync(new Snowflake(discordConfiguration.GuildId));

            if (!channels.IsSuccess)
            {
                return new List<IChannel>();
            }

            return channels.Entity;
        }
    }

    public void InvalidateGuildChannels() => appCache.Remove(GetGuildChannelsCacheKey());

    public IRole GetEveryoneRole() => appCache.Get<IRole>($"{nameof(DiscordCache)}/EveryoneRole");
    public void SetEveryoneRole(IRole role) => appCache.Add($"{nameof(DiscordCache)}/EveryoneRole", role, _neverExpireCacheEntryOptions);

    public async Task<Result<IGuildMember>> GetGuildMember(ulong discordUserId) 
    {
        return await appCache.GetOrAddAsync(
            $"{nameof(GetGuildMember)}/{discordConfiguration.GuildId}/{discordUserId}",
            () => guildApi.GetGuildMemberAsync(new Snowflake(discordConfiguration.GuildId), new Snowflake(discordUserId)),
            TimeSpan.FromMinutes(5));
    }
}
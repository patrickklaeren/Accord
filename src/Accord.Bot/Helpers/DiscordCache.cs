using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Accord.Bot.Infrastructure;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Helpers;

[AutoConstructor]
public partial class DiscordCache
{
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IAppCache _appCache;
    private readonly MemoryCacheEntryOptions _neverExpireCacheEntryOptions = new()
    {
        Priority = CacheItemPriority.NeverRemove
    };

    private readonly DiscordConfiguration _discordConfiguration;

    public Snowflake GetSelfSnowflake() => _appCache.Get<Snowflake>($"{nameof(DiscordCache)}/SelfSnowflake");
    public void SetSelfSnowflake(Snowflake snowflake) => _appCache.Add($"{nameof(DiscordCache)}/SelfSnowflake", snowflake);

    public IGuildMember GetGuildSelfMember() => _appCache.Get<IGuildMember>($"{nameof(DiscordCache)}/SelfMember");
    public void SetGuildSelfMember(IGuildMember guildMember) => _appCache.Add($"{nameof(DiscordCache)}/SelfMember", guildMember, _neverExpireCacheEntryOptions);

    private string GetGuildRolesCacheKey() => nameof(DiscordCache) + "/Roles";

    public async Task<IReadOnlyList<IRole>> GetGuildRoles()
    {
        return await _appCache.GetOrAddAsync(GetGuildRolesCacheKey(), GetGuildRolesData, _neverExpireCacheEntryOptions);

        async Task<IReadOnlyList<IRole>> GetGuildRolesData()
        {
            var guild = await _guildApi.GetGuildAsync(new Snowflake(_discordConfiguration.GuildId), true);

            if (!guild.IsSuccess)
            {
                new List<IRole>();
            }

            return guild.Entity.Roles;
        }
    }

    public void InvalidateGuildRoles() => _appCache.Remove(GetGuildRolesCacheKey());
    private string GetGuildChannelsCacheKey() => nameof(DiscordCache) + "/Channels";

    public async Task<IReadOnlyList<IChannel>> GetGuildChannels()
    {
        return await _appCache.GetOrAddAsync(GetGuildChannelsCacheKey(), GetGuildChannelsData, _neverExpireCacheEntryOptions);

        async Task<IReadOnlyList<IChannel>> GetGuildChannelsData()
        {
            var channels = await _guildApi.GetGuildChannelsAsync(new Snowflake(_discordConfiguration.GuildId));

            if (!channels.IsSuccess)
            {
                return new List<IChannel>();
            }

            return channels.Entity;
        }
    }

    public void InvalidateGuildChannels() => _appCache.Remove(GetGuildChannelsCacheKey());

    public IRole GetEveryoneRole() => _appCache.Get<IRole>($"{nameof(DiscordCache)}/EveryoneRole");
    public void SetEveryoneRole(IRole role) => _appCache.Add($"{nameof(DiscordCache)}/EveryoneRole", role, _neverExpireCacheEntryOptions);

    public async Task<Result<IGuildMember>> GetGuildMember(ulong discordUserId) 
    {
        return await _appCache.GetOrAddAsync(
            $"{nameof(GetGuildMember)}/{_discordConfiguration.GuildId}/{discordUserId}",
            () => _guildApi.GetGuildMemberAsync(new Snowflake(_discordConfiguration.GuildId), new Snowflake(discordUserId)),
            TimeSpan.FromMinutes(5));
    }
}
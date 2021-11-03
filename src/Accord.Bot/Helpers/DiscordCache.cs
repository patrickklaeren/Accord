using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.Helpers;

public class DiscordCache
{
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IAppCache _appCache;
    private readonly MemoryCacheEntryOptions _neverExpireCacheEntryOptions;

    public DiscordCache(IDiscordRestGuildAPI guildApi, IAppCache appCache)
    {
        _guildApi = guildApi;
        _appCache = appCache;
        _neverExpireCacheEntryOptions = new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.NeverRemove
        };
    }

    public Snowflake GetSelfSnowflake() => _appCache.Get<Snowflake>("SelfSnowflake");
    public void SetSelfSnowflake(Snowflake snowflake) => _appCache.Add("SelfSnowflake", snowflake);

    public IGuildMember GetGuildSelfMember(ulong guildId) => _appCache.Get<IGuildMember>($"{guildId}/SelfMember");
    public void SetGuildSelfMember(Snowflake guildId, IGuildMember guildMember) => _appCache.Add($"{guildId.Value}/SelfMember", guildMember, _neverExpireCacheEntryOptions);

    public IRole GetEveryoneRole(ulong guildId) => _appCache.Get<IRole>($"{guildId}/EveryoneRole");
    public IRole GetEveryoneRole(Snowflake guildId) => GetEveryoneRole(guildId.Value);
    public void SetEveryoneRole(Snowflake guildId, IRole role) => _appCache.Add($"{guildId.Value}/EveryoneRole", role, _neverExpireCacheEntryOptions);

    public async Task<Result<IGuildMember>> GetGuildMember(ulong discordGuildId, ulong discordUserId) =>
        await _appCache.GetOrAddAsync(
            $"{nameof(GetGuildMember)}/{discordGuildId}/{discordUserId}",
            () => _guildApi.GetGuildMemberAsync(new Snowflake(discordGuildId), new Snowflake(discordUserId)),
            TimeSpan.FromMinutes(5));
}
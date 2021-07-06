using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.Helpers
{
    public class DiscordCache
    {
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IAppCache _appCache;

        public DiscordCache(IDiscordRestGuildAPI guildApi, IAppCache appCache)
        {
            _guildApi = guildApi;
            _appCache = appCache;
        }

        public Snowflake GetSelfSnowflake() => _appCache.Get<Snowflake>("SelfSnowflake");

        public void SetSelfSnowflake(Snowflake snowflake) => _appCache.Add("SelfSnowflake", snowflake);

        public async Task<IRole> GetEveryoneRole(Snowflake guildSnowflake)
        {
            return await _appCache.GetOrAddAsync("EveryoneRole", 
                () => GetEveryoneRoleInternal(guildSnowflake), 
                DateTimeOffset.Now.AddDays(30));
        }

        private async Task<IRole> GetEveryoneRoleInternal(Snowflake guildSnowflake)
        {
            var roles = await _guildApi.GetGuildRolesAsync(guildSnowflake);

            if (!roles.IsSuccess)
            {
                throw new InvalidOperationException("Cannot get everyone role for guild");
            }

            return roles.Entity.Single(x => x.Name == "@everyone");
        }

        public async Task<Result<IGuildMember>> GetGuildMember(ulong discordGuildId, ulong discordUserId)
        {
            return await _appCache.GetOrAddAsync($"{nameof(GetGuildMember)}/{discordGuildId}/{discordUserId}",
                () => _guildApi.GetGuildMemberAsync(new Snowflake(discordGuildId), new Snowflake(discordUserId)), 
                DateTimeOffset.Now.AddMinutes(5));
        }
        
        public async Task<Result<IReadOnlyList<IRole>>> GetRoles(ulong discordGuildId)
        {
            return await _appCache.GetOrAddAsync($"{nameof(GetRoles)}/{discordGuildId}",
                () => _guildApi.GetGuildRolesAsync(new Snowflake(discordGuildId)), 
                DateTimeOffset.Now.AddMinutes(5));
        }
    }
}

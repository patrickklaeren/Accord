using System;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;

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
    }
}

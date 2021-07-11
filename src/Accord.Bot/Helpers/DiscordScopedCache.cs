using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.Helpers
{
    public class DiscordScopedCache : DiscordCache
    {
        private readonly ICommandContext _commandContext;

        public DiscordScopedCache(ICommandContext commandContext, IDiscordRestGuildAPI guildApi, IAppCache appCache)
            : base(guildApi, appCache)
        {
            _commandContext = commandContext;
        }

        public IGuildMember GetGuildSelfMember() => GetGuildSelfMember(_commandContext.GuildID.Value);
        public IReadOnlyList<IRole> GetGuildRoles() => GetGuildRoles(_commandContext.GuildID.Value);
        public IReadOnlyList<IChannel> GetGuildChannels() => GetGuildChannels(_commandContext.GuildID.Value);
        public IRole GetEveryoneRole() => GetEveryoneRole(_commandContext.GuildID.Value);
        public Task<Result<IGuildMember>> GetGuildMember(ulong discordUserId) => GetGuildMember(_commandContext.GuildID.Value.Value, discordUserId);
        public async Task<Result<IGuildMember>> GetInvokingGuildMember() => await GetGuildMember(_commandContext.GuildID.Value.Value, _commandContext.User.ID.Value);
    }
}
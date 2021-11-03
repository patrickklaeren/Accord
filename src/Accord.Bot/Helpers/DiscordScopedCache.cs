using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.Helpers;

public class DiscordScopedCache : DiscordCache
{
    private readonly ICommandContext _commandContext;

    public DiscordScopedCache(ICommandContext commandContext, IDiscordRestGuildAPI guildApi, IAppCache appCache)
        : base(guildApi, appCache)
    {
        _commandContext = commandContext;
    }

    public IRole GetEveryoneRole() => GetEveryoneRole(_commandContext.GuildID.Value);
    public async Task<Result<IGuildMember>> GetInvokingGuildMember() => await GetGuildMember(_commandContext.GuildID.Value.Value, _commandContext.User.ID.Value);
}
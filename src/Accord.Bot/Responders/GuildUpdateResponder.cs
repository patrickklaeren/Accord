using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class GuildUpdateResponder(DiscordCache discordCache) : IResponder<IGuildUpdate>
{
    public Task<Result> RespondAsync(IGuildUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        discordCache.InvalidateGuildRoles();
        return Task.FromResult(Result.FromSuccess());
    }
}
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class GuildUpdateResponder : IResponder<IGuildUpdate>
{
    private readonly DiscordCache _discordCache;

    public GuildUpdateResponder(DiscordCache discordCache)
    {
        _discordCache = discordCache;
    }

    public Task<Result> RespondAsync(IGuildUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildRoles();
        return Task.FromResult(Result.FromSuccess());
    }
}
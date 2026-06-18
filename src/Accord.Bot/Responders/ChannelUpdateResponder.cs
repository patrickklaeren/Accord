using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ChannelUpdateResponder(DiscordCache discordCache) :
    IResponder<IChannelCreate>,
    IResponder<IChannelUpdate>,
    IResponder<IChannelDelete>
{

    public Task<Result> RespondAsync(IChannelCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        discordCache.InvalidateGuildChannels();
        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IChannelUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        discordCache.InvalidateGuildChannels();
        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IChannelDelete gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        discordCache.InvalidateGuildChannels();
        return Task.FromResult(Result.FromSuccess());
    }
}
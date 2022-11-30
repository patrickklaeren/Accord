using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

[AutoConstructor]
public partial class ChannelUpdateResponder :
    IResponder<IChannelCreate>,
    IResponder<IChannelUpdate>,
    IResponder<IChannelDelete>
{
    private readonly DiscordCache _discordCache;

    public Task<Result> RespondAsync(IChannelCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildChannels();
        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IChannelUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildChannels();
        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IChannelDelete gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildChannels();
        return Task.FromResult(Result.FromSuccess());
    }
}
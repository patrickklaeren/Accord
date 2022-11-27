using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ChannelUpdateResponder :
    IResponder<IChannelCreate>,
    IResponder<IChannelUpdate>,
    IResponder<IChannelDelete>
{
    private readonly DiscordCache _discordCache;

    public ChannelUpdateResponder(DiscordCache discordCache)
    {
        _discordCache = discordCache;
    }

    public Task<Result> RespondAsync(IChannelCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        var guildChannels = _discordCache.GetGuildChannels(gatewayEvent.GuildID.Value).ToList();
        guildChannels.Add(gatewayEvent);

        _discordCache.SetGuildChannels(gatewayEvent.GuildID.Value, guildChannels);

        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IChannelUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        var guildChannels = _discordCache
            .GetGuildChannels(gatewayEvent.GuildID.Value)
            .Where(x => x.ID != gatewayEvent.ID)
            .ToList();
        guildChannels.Add(gatewayEvent);

        _discordCache.SetGuildChannels(gatewayEvent.GuildID.Value, guildChannels);

        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IChannelDelete gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        var guildChannels = _discordCache
            .GetGuildChannels(gatewayEvent.GuildID.Value)
            .Where(x => x.ID != gatewayEvent.ID)
            .ToList();

        _discordCache.SetGuildChannels(gatewayEvent.GuildID.Value, guildChannels);

        return Task.FromResult(Result.FromSuccess());
    }
}
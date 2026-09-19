using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.VoiceChannelLeasing;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class VoiceChannelLeaseSeedResponder(
    VoiceChannelLeaseOccupancyTracker occupancyTracker,
    DiscordConfiguration discordConfiguration,
    IMediator mediator) : IResponder<IGuildCreate>
{
    public async Task<Result> RespondAsync(IGuildCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (!gatewayEvent.Guild.IsT0)
            return Result.FromSuccess();

        var guild = gatewayEvent.Guild.AsT0;

        if (guild.ID.Value != discordConfiguration.GuildId)
            return Result.FromSuccess();

        var activeLeases = await mediator.Send(new GetActiveVoiceChannelLeasesRequest(), ct);
        var leasedChannelIds = activeLeases.Select(x => x.DiscordChannelId).ToList();

        var voiceStates = new List<(ulong UserId, ulong? ChannelId)>();

        foreach (var voiceState in guild.VoiceStates)
        {
            if (!voiceState.UserID.HasValue)
                continue;

            if (voiceState.Member.HasValue
                && voiceState.Member.Value.User.HasValue
                && voiceState.Member.Value.User.Value.IsBot.HasValue
                && voiceState.Member.Value.User.Value.IsBot.Value)
                continue;

            var channelId = voiceState.ChannelID.HasValue ? voiceState.ChannelID.Value?.Value : null;

            voiceStates.Add((voiceState.UserID.Value.Value, channelId));
        }

        occupancyTracker.Seed(leasedChannelIds, voiceStates);

        return Result.FromSuccess();
    }
}

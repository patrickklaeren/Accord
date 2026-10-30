using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.AiAnalysis;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public sealed class AiMessageAnalysisResponder(
    AiMessageAnalysisQueue eventQueue,
    DiscordConfiguration discordConfiguration) : IResponder<IMessageCreate>
{
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        if (!gatewayEvent.GuildID.HasValue
            || gatewayEvent.GuildID.Value.Value != discordConfiguration.GuildId
            || (gatewayEvent.Author.IsBot.HasValue && gatewayEvent.Author.IsBot.Value)
            || (gatewayEvent.Author.IsSystem.HasValue && gatewayEvent.Author.IsSystem.Value)
            || gatewayEvent.WebhookID.HasValue
            || string.IsNullOrWhiteSpace(gatewayEvent.Content))
        {
            return Result.FromSuccess();
        }

        await eventQueue.Queue(new AnalyseMemberMessageRequest(
            gatewayEvent.ID.Value,
            gatewayEvent.Author.ID.Value,
            gatewayEvent.ChannelID.Value,
            gatewayEvent.Content), ct);

        return Result.FromSuccess();
    }
}

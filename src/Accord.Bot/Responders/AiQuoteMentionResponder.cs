using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Accord.Services;
using Accord.Services.AiQuotes;
using Accord.Services.UserBotMessages;
using MediatR;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public sealed class AiQuoteMentionResponder(
    DiscordConfiguration discordConfiguration,
    DiscordCache discordCache,
    IOptions<DiscordCommandResponderOptions> commandOptions,
    IDiscordRestChannelAPI channelApi,
    IMediator mediator) : IResponder<IMessageCreate>
{
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        if (!gatewayEvent.GuildID.HasValue
            || gatewayEvent.GuildID.Value.Value != discordConfiguration.GuildId
            || gatewayEvent.Author.IsBot is { HasValue: true, Value: true }
            || gatewayEvent.Author.IsSystem is { HasValue: true, Value: true }
            || gatewayEvent.WebhookID.HasValue
            || string.IsNullOrWhiteSpace(gatewayEvent.Content)
            || gatewayEvent.Mentions.Count == 0)
        {
            return Result.FromSuccess();
        }

        // Let prefixed commands handle their own responses, including !ai.
        var prefix = commandOptions.Value.Prefix;
        if (!string.IsNullOrEmpty(prefix)
            && gatewayEvent.Content.TrimStart().StartsWith(prefix, StringComparison.Ordinal))
        {
            return Result.FromSuccess();
        }

        var selfId = await discordCache.GetSelfSnowflake();
        if (!gatewayEvent.Mentions.Any(user => user.ID == selfId)
            || (!gatewayEvent.Content.Contains($"<@{selfId.Value}>", StringComparison.Ordinal)
                && !gatewayEvent.Content.Contains($"<@!{selfId.Value}>", StringComparison.Ordinal)))
        {
            return Result.FromSuccess();
        }

        var response = await mediator.Send(new GetAiQuoteRequest(), ct);
        var reply = await channelApi.CreateMessageAsync(
            gatewayEvent.ChannelID,
            response.Quote,
            messageReference: new MessageReference(
                MessageID: gatewayEvent.ID,
                ChannelID: gatewayEvent.ChannelID,
                GuildID: gatewayEvent.GuildID,
                FailIfNotExists: false),
            allowedMentions: new AllowedMentions(MentionRepliedUser: false),
            ct: ct);

        if (!reply.IsSuccess)
        {
            return Result.FromError(reply.Error);
        }

        await mediator.Publish(new AddUserBotMessageRequest(
            reply.Entity.ID.Value,
            reply.Entity.ChannelID.Value,
            gatewayEvent.Author.ID.Value), ct);

        return Result.FromSuccess();
    }
}

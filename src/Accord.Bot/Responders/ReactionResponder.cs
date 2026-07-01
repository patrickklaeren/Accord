using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.DemocraticDownVoting;
using Accord.Services.Helpers;
using Accord.Services.Starboard;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ReactionResponder(
    IMediator mediator,
    DiscordCache discordCache,
    PermissionUserFactory permissionUserFactory,
    IDiscordRestChannelAPI channelApi,
    StarboardEventQueue starboardEventQueue,
    DemocraticDownVotingEventQueue democraticDownVotingEventQueue)
    : IResponder<IMessageReactionAdd>,
        IResponder<IMessageReactionRemove>
{
    public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = new())
    {
        var emoji = gatewayEvent.Emoji;

        var emojiString = emoji.Name.HasValue
            ? emoji.Name
            : emoji.IsAnimated is { HasValue: true, Value: true }
                ? $"<a:{emoji.Name}:{emoji.ID.Value}>"
                : $"<:{emoji.Name}:{emoji.ID.Value}>";

        if ((emojiString.Value is "❌" or "🗑️") && gatewayEvent.MessageAuthorID.HasValue)
        {
            if (gatewayEvent.MessageAuthorID.HasValue
                && gatewayEvent.MessageAuthorID.Value == discordCache.GetSelfSnowflake())
            {
                var permissionUser = await permissionUserFactory.FromId(gatewayEvent.UserID.Value);
                await mediator.Publish(new DeleteUserBotMessageRequest(permissionUser, gatewayEvent.MessageID.Value), ct);
            }
        }
        else if (emojiString.Value is DemocraticDownVotingConstants.EMOJI && gatewayEvent.MessageAuthorID.HasValue)
        {
            var authorUser = await permissionUserFactory.FromId(gatewayEvent.MessageAuthorID.Value.Value);
            var timestamp = DiscordSnowflakeHelper.ToDateTimeOffset(gatewayEvent.MessageID.Value);
            
            await democraticDownVotingEventQueue.Queue(new DownVoteMessageRequest(authorUser,
                timestamp,
                gatewayEvent.MessageID.Value,
                gatewayEvent.ChannelID.Value));
        }
        else if (emojiString.Value is StarboardConstants.EMOJI)
        {
            var channel = await channelApi.GetChannelAsync(gatewayEvent.ChannelID, ct);

            if (channel.IsSuccess)
            {
                await starboardEventQueue.Queue(new StarMessageRequest(gatewayEvent.MessageID.Value,
                    gatewayEvent.ChannelID.Value,
                    channel.Entity.ParentID.HasValue ? channel.Entity.ParentID.Value?.Value : null,
                    gatewayEvent.UserID.Value));
            }
        }

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        var emoji = gatewayEvent.Emoji;

        var emojiString = emoji.Name.HasValue
            ? emoji.Name
            : emoji.IsAnimated is { HasValue: true, Value: true }
                ? $"<a:{emoji.Name}:{emoji.ID.Value}>"
                : $"<:{emoji.Name}:{emoji.ID.Value}>";

        if (emojiString.Value is StarboardConstants.EMOJI)
        {
            var channel = await channelApi.GetChannelAsync(gatewayEvent.ChannelID, ct);

            if (channel.IsSuccess)
            {
                await starboardEventQueue.Queue(new StarMessageRequest(gatewayEvent.MessageID.Value,
                    gatewayEvent.ChannelID.Value,
                    channel.Entity.ParentID.HasValue ? channel.Entity.ParentID.Value?.Value : null,
                    gatewayEvent.UserID.Value));
            }
        }

        return Result.FromSuccess();
    }
}
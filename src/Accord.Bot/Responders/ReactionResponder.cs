using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Starboard;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ReactionResponder(IMediator mediator, 
    PermissionUserFactory permissionUserFactory,
    StarboardEventQueue eventQueue) 
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

        if (emojiString.Value is "❌" or "🗑️")
        {
            var permissionUser = await permissionUserFactory.FromId(gatewayEvent.UserID.Value);
            await mediator.Publish(new DeleteUserBotMessageRequest(permissionUser, gatewayEvent.MessageID.Value), ct);
        }
        else if (StarboardConstants.Emojis.Contains(emojiString.Value))
        {
            await eventQueue.Queue(new StarMessageRequest(gatewayEvent.MessageID.Value, 
                gatewayEvent.ChannelID.Value, 
                gatewayEvent.UserID.Value));    
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
        
        if (StarboardConstants.Emojis.Contains(emojiString.Value))
        {
            await eventQueue.Queue(new StarMessageRequest(gatewayEvent.MessageID.Value, 
                gatewayEvent.ChannelID.Value, 
                gatewayEvent.UserID.Value));    
        }
        
        return Result.FromSuccess();
    }
}
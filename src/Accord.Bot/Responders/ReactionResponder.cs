using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ReactionResponder(IMediator mediator, PermissionUserFactory permissionUserFactory) : IResponder<IMessageReactionAdd>
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

        return Result.FromSuccess();
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class TagResponder(TagHelper tagHelper, IDiscordRestChannelAPI channelApi, IMediator mediator) : IResponder<IMessageCreate>
{
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
            return Result.FromSuccess();

        var tag = await tagHelper.TryGetTag(gatewayEvent.Content);

        if (tag is not null)
        {
            var reply = await channelApi.CreateMessageAsync(gatewayEvent.ChannelID,
                tag,
                messageReference: gatewayEvent.MessageReference,
                allowedMentions: new AllowedMentions(Parse: new List<MentionType>()),
                ct: ct);

            if (reply.IsSuccess)
            {
                await mediator.Publish(new AddUserBotMessageRequest(reply.Entity.ID.Value, reply.Entity.ChannelID.Value, gatewayEvent.Author.ID.Value), ct);
            }
        }

        return Result.FromSuccess();
    }
}
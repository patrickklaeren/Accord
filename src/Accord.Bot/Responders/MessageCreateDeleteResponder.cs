using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.UserMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class MessageCreateDeleteResponder : IResponder<IMessageCreate>, 
        IResponder<IMessageDelete>, 
        IResponder<IMessageDeleteBulk>
    {
        private readonly IMediator _mediator;

        public MessageCreateDeleteResponder(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
                return Result.FromSuccess();

            var channelsIgnored = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromMessageTracking), ct);

            if (channelsIgnored.Any(id => id == gatewayEvent.ChannelID.Value))
                return Result.FromSuccess();

            await _mediator.Send(new AddMessageRequest(gatewayEvent.ID.Value, gatewayEvent.Author.ID.Value, 
                gatewayEvent.ChannelID.Value, gatewayEvent.Timestamp), ct);

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var channelsIgnored = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromMessageTracking), ct);

            if (channelsIgnored.Any(id => id == gatewayEvent.ChannelID.Value))
                return Result.FromSuccess();

            await _mediator.Send(new DeleteMessageRequest(gatewayEvent.ID.Value), ct);
            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDeleteBulk gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var channelsIgnored = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromMessageTracking), ct);

            if (channelsIgnored.Any(id => id == gatewayEvent.ChannelID.Value))
                return Result.FromSuccess();

            foreach (var id in gatewayEvent.IDs)
            {
                await _mediator.Send(new DeleteMessageRequest(id.Value), ct);
            }

            return Result.FromSuccess();
        }
    }
}

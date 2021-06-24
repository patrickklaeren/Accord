using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class XpResponder : IResponder<IMessageCreate>
    {
        private readonly IEventQueue _eventQueue;
        private readonly IMediator _mediator;

        public XpResponder(IEventQueue eventQueue, IMediator mediator)
        {
            _eventQueue = eventQueue;
            _mediator = mediator;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
                return Result.FromSuccess();

            var channelsIgnored = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromXp), ct);

            if (channelsIgnored.Any(id => id == gatewayEvent.ChannelID.Value))
                return Result.FromSuccess();

            await _eventQueue.Queue(new CalculateXpForUserEvent(gatewayEvent.Author.ID.Value,
                gatewayEvent.ChannelID.Value, gatewayEvent.Timestamp));

            return Result.FromSuccess();
        }
    }
}

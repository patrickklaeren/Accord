using System.Threading;
using System.Threading.Tasks;
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
            await _mediator.Send(new AddMessageRequest(gatewayEvent.ID.Value, gatewayEvent.Author.ID.Value, 
                gatewayEvent.ChannelID.Value, gatewayEvent.Timestamp), ct);

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            await _mediator.Send(new DeleteMessageRequest(gatewayEvent.ID.Value), ct);

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDeleteBulk gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            foreach (var id in gatewayEvent.IDs)
            {
                await _mediator.Send(new DeleteMessageRequest(id.Value), ct);
            }

            return Result.FromSuccess();
        }
    }
}

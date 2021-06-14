using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class XpResponder : IResponder<IMessageCreate>
    {
        private readonly IEventQueue _eventQueue;

        public XpResponder(IEventQueue eventQueue)
        {
            _eventQueue = eventQueue;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (!gatewayEvent.Author.IsBot.HasValue
                && !gatewayEvent.Author.IsSystem.HasValue)
            {
                await _eventQueue.Queue(new MessageSentEvent(gatewayEvent.Author.ID.Value, 
                    gatewayEvent.ChannelID.Value, gatewayEvent.Timestamp));
            }

            return Result.FromSuccess();
        }
    }
}

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
        private readonly IXpCalculatorQueueService _xpCalculatorQueueService;

        public XpResponder(IXpCalculatorQueueService xpCalculatorQueueService)
        {
            _xpCalculatorQueueService = xpCalculatorQueueService;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (gatewayEvent.Author.IsBot.HasValue
                && gatewayEvent.Author.IsBot.Value)
            {
                return Result.FromSuccess();
            }
            
            await _xpCalculatorQueueService.QueueBackgroundWorkItemAsync(new XpCalculationForUser(gatewayEvent.Author.ID.Value, gatewayEvent.Timestamp));
            return Result.FromSuccess();
        }
    }
}

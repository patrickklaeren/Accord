using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Accord.Services
{
    public interface IXpCalculatorQueueService
    {
        ValueTask QueueBackgroundWorkItemAsync(XpCalculationForUser workItem);
        ValueTask<XpCalculationForUser> DequeueAsync(CancellationToken cancellationToken);
    }

    public class XpCalculatorQueueService : IXpCalculatorQueueService
    {
        private const int QUEUE_CAPACITY = 1000;
        private readonly Channel<XpCalculationForUser> _queue;

        public XpCalculatorQueueService()
        {
            var options = new BoundedChannelOptions(QUEUE_CAPACITY)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };

            _queue = Channel.CreateBounded<XpCalculationForUser>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(XpCalculationForUser workItem)
        {
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<XpCalculationForUser> DequeueAsync(
            CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }

    public record XpCalculationForUser(ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset MessageSentDateTime);
}

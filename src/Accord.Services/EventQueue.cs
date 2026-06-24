using MediatR;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Accord.Services;

public interface IEventQueue
{
    ValueTask Queue(IRequest queuedEvent);
    ValueTask<IRequest> Dequeue(CancellationToken cancellationToken);
}

public class EventQueue : IEventQueue
{
    private const int QUEUE_CAPACITY = 1000;
    private readonly Channel<IRequest> _queue;

    public EventQueue()
    {
        var options = new BoundedChannelOptions(QUEUE_CAPACITY)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        };

        _queue = Channel.CreateBounded<IRequest>(options);
    }

    public async ValueTask Queue(IRequest queuedEvent)
    {
        await _queue.Writer.WriteAsync(queuedEvent);
    }

    public async ValueTask<IRequest> Dequeue(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

public interface IEnsureUserExistsRequest : IRequest
{
    ulong DiscordUserId { get; }
}
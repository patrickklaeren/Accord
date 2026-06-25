using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services;

[RegisterSingleton(typeof(CoreEventQueue))]
public class CoreEventQueue : IEventQueue
{
    private readonly EventQueue _queue = new();
    public ValueTask Queue(IRequest queuedEvent) => _queue.Queue(queuedEvent);
    public ValueTask<IRequest> Dequeue(CancellationToken cancellationToken) => _queue.Dequeue(cancellationToken);
}
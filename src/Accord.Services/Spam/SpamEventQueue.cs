using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Spam;

[RegisterSingleton(typeof(SpamEventQueue))]
public class SpamEventQueue
{
    private readonly EventQueue _queue = new();
    public ValueTask Queue(IRequest queuedEvent) => _queue.Queue(queuedEvent);
    public ValueTask<IRequest> Dequeue(CancellationToken cancellationToken) => _queue.Dequeue(cancellationToken);
}
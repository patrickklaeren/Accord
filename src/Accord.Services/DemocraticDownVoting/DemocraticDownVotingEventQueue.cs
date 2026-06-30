using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.DemocraticDownVoting;

[RegisterSingleton(typeof(DemocraticDownVotingEventQueue))]
public class DemocraticDownVotingEventQueue
{
    private readonly EventQueue _queue = new();
    public ValueTask Queue(IRequest queuedEvent) => _queue.Queue(queuedEvent);
    public ValueTask<IRequest> Dequeue(CancellationToken cancellationToken) => _queue.Dequeue(cancellationToken);
}
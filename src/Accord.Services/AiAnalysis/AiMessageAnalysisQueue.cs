using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Accord.Services.AiAnalysis;

[RegisterSingleton(typeof(AiMessageAnalysisQueue))]
public sealed class AiMessageAnalysisQueue
{
    private const int QUEUE_CAPACITY = 1000;

    private readonly Channel<AnalyseMemberMessageRequest> _queue =
        Channel.CreateBounded<AnalyseMemberMessageRequest>(new BoundedChannelOptions(QUEUE_CAPACITY)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ValueTask Queue(AnalyseMemberMessageRequest request, CancellationToken cancellationToken) =>
        _queue.Writer.WriteAsync(request, cancellationToken);

    public ValueTask<AnalyseMemberMessageRequest> Dequeue(CancellationToken cancellationToken) =>
        _queue.Reader.ReadAsync(cancellationToken);
}

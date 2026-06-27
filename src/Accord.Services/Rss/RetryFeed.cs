using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Rss;

public sealed record RetryFeedRequest(int RssFeedId) : IRequest;

public class RetryFeedHandler(RssService rssService) : IRequestHandler<RetryFeedRequest>
{
    public async Task Handle(RetryFeedRequest request, CancellationToken cancellationToken)
    {
        await rssService.RetryFeed(request.RssFeedId, cancellationToken);
    }
}

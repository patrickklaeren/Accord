using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Rss;

public sealed record RemoveFeedRequest(int RssFeedId) : IRequest;

public class RemoveFeedHandler(RssService rssService) : IRequestHandler<RemoveFeedRequest>
{
    public async Task Handle(RemoveFeedRequest request, CancellationToken cancellationToken)
    {
        await rssService.RemoveFeed(request.RssFeedId, cancellationToken);
    }
}

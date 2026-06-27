using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Rss;

public sealed record GetFeedIdsToReadRequest : IRequest<IReadOnlyCollection<int>>;

public class GetFeedIdsToReadHandler(RssService rssService) : IRequestHandler<GetFeedIdsToReadRequest, IReadOnlyCollection<int>>
{
    public async Task<IReadOnlyCollection<int>> Handle(GetFeedIdsToReadRequest request, CancellationToken cancellationToken)
    {
        return await rssService.GetFeedIdsToRead(cancellationToken);
    }
}

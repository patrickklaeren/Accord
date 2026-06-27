using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Rss;

public sealed record AddFeedRequest(ulong DiscordChannelId, string Url) : IRequest;

public class AddFeedHandler(RssService rssService) : IRequestHandler<AddFeedRequest>
{
    public async Task Handle(AddFeedRequest request, CancellationToken cancellationToken)
    {
        await rssService.AddFeed(request.DiscordChannelId, request.Url, cancellationToken);
    }
}

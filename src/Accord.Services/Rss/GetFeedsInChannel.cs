using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Rss;

public sealed record GetFeedsInChannelRequest(ulong DiscordChannelId) : IRequest<IReadOnlyCollection<RssFeedInChannelDto>>;

public class GetFeedsInChannelHandler(RssService rssService) : IRequestHandler<GetFeedsInChannelRequest, IReadOnlyCollection<RssFeedInChannelDto>>
{
    public async Task<IReadOnlyCollection<RssFeedInChannelDto>> Handle(GetFeedsInChannelRequest request, CancellationToken cancellationToken)
    {
        return await rssService.GetFeedsInChannel(request.DiscordChannelId, cancellationToken);
    }
}

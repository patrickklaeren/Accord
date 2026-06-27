using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Rss;

public sealed record GetNewPostsFromFeedRequest(int RssFeedId) : IRequest<FeedWithNewPostsDto>;
public sealed record FeedWithNewPostsDto(ulong DiscordChannelId, IReadOnlyCollection<RssFeedItemDto> NewPosts);

internal class GetNewPostsFromFeedHandler(RssService rssService, AccordContext db) : IRequestHandler<GetNewPostsFromFeedRequest, FeedWithNewPostsDto>
{
    public async Task<FeedWithNewPostsDto> Handle(GetNewPostsFromFeedRequest request, CancellationToken cancellationToken)
    {
        var channelId = await db.RssFeeds
            .Where(x => x.Id == request.RssFeedId)
            .Select(x => x.DiscordChannelId)
            .SingleOrDefaultAsync(cancellationToken);

        var newPosts = await rssService.GetNewPostsFromFeed(request.RssFeedId, cancellationToken);
        return new FeedWithNewPostsDto(channelId, newPosts);
    }
}

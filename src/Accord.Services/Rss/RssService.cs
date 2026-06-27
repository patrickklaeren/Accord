using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Rss;

[RegisterScoped]
public class RssService(AccordContext db, RssFeedReaderService rssFeedReaderService)
{
    private const int MAX_FAILED_FETCHES = 5;
    private readonly TimeSpan _fetchInterval = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyCollection<int>> GetFeedIdsToRead(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await db
            .RssFeeds
            .Where(x => x.NextFetchDateTime != null)
            .Where(x => x.NextFetchDateTime <= now)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<RssFeedItemDto>> GetNewPostsFromFeed(int rssFeedId,
        CancellationToken cancellationToken)
    {
        var feed = await db.RssFeeds
            .Where(x => x.Id == rssFeedId)
            .SingleAsync(cancellationToken: cancellationToken);

        var postsToReturn = new List<RssFeedItemDto>();
        var result = await rssFeedReaderService.GetFeed(feed.Url);

        if (!result.Success)
        {
            feed.LastFailedFetchResponse = result.ErrorMessage;
            feed.NumberOfFailedFetches++;
        }
        else
        {
            foreach (var postInFeed in result.Value!)
            {
                if (postInFeed.PublishedAt < feed.AddedDateTime)
                    continue;

                var hasBeenPosted = await db.RssFeedPosts
                    .Where(x => x.Url == postInFeed.Url)
                    .Where(x => x.Title == postInFeed.Title)
                    .Where(x => x.PublishedAtDateTime == postInFeed.PublishedAt)
                    .AnyAsync(cancellationToken: cancellationToken);

                if (hasBeenPosted)
                    continue;

                var post = new RssFeedPost
                {
                    Title = postInFeed.Title,
                    Url = postInFeed.Url,
                    PublishedAtDateTime = postInFeed.PublishedAt,
                };

                feed.Posts.Add(post);
                postsToReturn.Add(postInFeed);
            }

            feed.NumberOfFailedFetches = 0;
            feed.LastFailedFetchResponse = null;
        }

        feed.NextFetchDateTime = feed.NumberOfFailedFetches >= MAX_FAILED_FETCHES 
            ? null 
            : GetNextFetchDateTime();

        await db.SaveChangesAsync(cancellationToken);

        return postsToReturn;
    }

    public async Task<IReadOnlyCollection<RssFeedInChannelDto>> GetFeedsInChannel(ulong discordChannelId, CancellationToken cancellationToken)
    {
        return await db.RssFeeds
            .Where(x => x.DiscordChannelId == discordChannelId)
            .Select(x => new RssFeedInChannelDto(x.Id, x.Url, x.NextFetchDateTime, x.NumberOfFailedFetches, x.LastFailedFetchResponse))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task AddFeed(ulong discordChannelId, string url, CancellationToken cancellationToken)
    {
        var hasFeedInChannel = await db.RssFeeds
            .Where(x => x.Url == url)
            .Where(x => x.DiscordChannelId == discordChannelId)
            .AnyAsync(cancellationToken: cancellationToken);

        if (hasFeedInChannel)
            return;

        var feed = new RssFeed
        {
            AddedDateTime = DateTimeOffset.UtcNow,
            NextFetchDateTime = DateTimeOffset.UtcNow,
            DiscordChannelId = discordChannelId,
            Url = url,
        };

        db.RssFeeds.Add(feed);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFeed(int rssFeedId, CancellationToken cancellationToken)
    {
        await db.RssFeedPosts
            .Where(x => x.RssFeedId == rssFeedId)
            .ExecuteDeleteAsync(cancellationToken);

        await db.RssFeeds
            .Where(x => x.Id == rssFeedId)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task RetryFeed(int rssFeedId, CancellationToken cancellationToken)
    {
        var nextTime = DateTimeOffset.UtcNow;

        await db.RssFeeds
            .Where(x => x.Id == rssFeedId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(d => d.NextFetchDateTime, nextTime), cancellationToken);
    }

    private DateTimeOffset GetNextFetchDateTime()
    {
        return DateTimeOffset.UtcNow.Add(_fetchInterval);
    }
}

public sealed record RssFeedInChannelDto(int Id, 
    string Url, 
    DateTimeOffset? NextFetchDateTime, 
    int NumberOfFailedFetches, 
    string? LastFailedFetchResponse);
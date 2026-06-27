using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Rss;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("rss")]
public class RssCommandGroup(ICommandContext commandContext,
    IMediator mediator,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("list")]
    [Description("Lists all RSS feeds in this channel")]
    [RequireDiscordPermission(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> ListFeeds()
    {
        commandContext.TryGetChannelID(out var channelId);

        var feeds = await mediator.Send(new GetFeedsInChannelRequest(channelId.Value));

        if (feeds.Count == 0)
        {
            return await feedbackService.SendContextualAsync("No RSS feeds in this channel");
        }

        var fields = feeds.Select(feed =>
        {
            var nextFetch = feed.NextFetchDateTime.HasValue
                ? DiscordFormatter.TimeToMarkdown(feed.NextFetchDateTime.Value)
                : "Disabled (too many failures)";

            var status = feed.NumberOfFailedFetches > 0
                ? $"⚠️ {feed.NumberOfFailedFetches} failed fetches"
                : "✅ Active";

            var detail = $"URL: {feed.Url}\nNext fetch: {nextFetch}\nStatus: {status}";

            if (!string.IsNullOrWhiteSpace(feed.LastFailedFetchResponse))
            {
                detail += $"\nLast error: {DiscordFormatter.TruncateToEmbedField(feed.LastFailedFetchResponse)}";
            }

            return new EmbedField($"Feed #{feed.Id}", DiscordFormatter.TruncateToEmbedField(detail), false);
        }).ToArray();

        var embed = new Embed(
            Title: $"RSS Feeds ({feeds.Count} total)",
            Fields: fields
        );

        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    [Command("add")]
    [Description("Adds an RSS feed URL to this channel")]
    [RequireDiscordPermission(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> AddFeed(string url)
    {
        commandContext.TryGetChannelID(out var channelId);

        await mediator.Send(new AddFeedRequest(channelId.Value, url));

        return await feedbackService.SendContextualAsync("RSS feed added to this channel");
    }

    [Command("remove")]
    [Description("Removes an RSS feed by its ID")]
    [RequireDiscordPermission(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> RemoveFeed(int id)
    {
        await mediator.Send(new RemoveFeedRequest(id));

        return await feedbackService.SendContextualAsync($"RSS feed #{id} removed");
    }

    [Command("retry")]
    [Description("Resets the retry timer on a failed RSS feed by its ID")]
    [RequireDiscordPermission(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> RetryFeed(int id)
    {
        await mediator.Send(new RetryFeedRequest(id));

        return await feedbackService.SendContextualAsync($"RSS feed #{id} will retry on next poll");
    }
}

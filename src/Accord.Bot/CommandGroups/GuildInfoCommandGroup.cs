using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Guilds;
using Accord.Services.Helpers;
using LazyCache;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public sealed class GuildInfoCommandGroup(
    IDiscordRestGuildAPI guildApi,
    DiscordConfiguration configuration,
    IMediator mediator,
    IAppCache cache,
    ICommandContext commandContext,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("guildinfo"), Description("Get information and participation statistics for this guild")]
    public async Task<IResult> GetGuildInfo()
    {
        var guildId = commandContext.GetCommandProxy().GuildId;

        if (guildId.Value != configuration.GuildId)
            return await feedbackService.SendContextualAsync("Guild statistics are only available for the configured guild.", ct: CancellationToken);

        var now = DateTimeOffset.UtcNow;
        var guildResult = await guildApi.GetGuildAsync(guildId, true, ct: CancellationToken);

        if (!guildResult.IsSuccess)
            return await feedbackService.SendContextualAsync("Could not retrieve guild information via the Discord API. Please try again later.", ct: CancellationToken);

        var guild = guildResult.Entity;
        var participation = await mediator.Send(new GetGuildParticipationRequest(now), CancellationToken);
        var memberCacheKey = $"GuildInfo/Members/{guildId.Value}";
        var members = await cache.GetOrAddAsync(memberCacheKey,
            () => GetMemberCounts(guildId, CancellationToken), TimeSpan.FromMinutes(5));

        if (members is null)
            cache.Remove(memberCacheKey);

        var activity = new StringBuilder()
            .AppendLine($"Last 7 days: {participation.MessagesLast7Days:N0} messages")
            .AppendLine($"Last 30 days: {participation.MessagesLast30Days:N0} messages")
            .AppendLine($"Avg. per day: {participation.MessagesLast30Days / 30.0:N1} messages");

        activity.Append(participation.MostActiveChannelId is { } channelId
            ? $"Most active channel: {DiscordFormatter.ChannelIdToMention(channelId)} ({participation.MostActiveChannelMessages:N0} messages in 30 days)"
            : "Most active channel: No messages recorded in the last 30 days");

        var memberInformation = members is not null
            ? $"Total members: {members.Total:N0}\nHumans: {members.Total - members.Bots:N0}\nBots: {members.Bots:N0}"
            : guild.ApproximateMemberCount.HasValue
                ? $"Approximate member count: {guild.ApproximateMemberCount.Value:N0}\nHuman/bot breakdown is currently unavailable."
                : "Member counts are currently unavailable.";

        var iconUrl = guild.Icon is { } icon
            ? $"{configuration.CdnBaseUrl.TrimEnd('/')}/icons/{guild.ID.Value}/{icon.Value}.{(icon.HasGif ? "gif" : "png")}?size=128"
            : null;

        var embed = new Embed(
            Author: new EmbedAuthor(guild.Name, IconUrl: iconUrl is null ? default : new Optional<string>(iconUrl)),
            Thumbnail: iconUrl is null ? default : new Optional<IEmbedThumbnail>(new EmbedThumbnail(iconUrl)),
            Timestamp: now,
            Fields: new IEmbedField[]
            {
                new EmbedField("Guild Information",
                    $"ID: {guild.ID.Value}\nOwner: {guild.OwnerID.ToUserMention()}\nCreated: {DiscordSnowflakeHelper.ToDateTimeOffset(guild.ID.Value).ToTimeMarkdown(TimeToMentionType.RelativeTime)}"),
                new EmbedField("Guild Participation", activity.ToString()),
                new EmbedField("Member Information", memberInformation),
                new EmbedField("Guild Roles", FormatRoles(guild))
            });

        return await feedbackService.SendContextualEmbedAsync(embed, ct: CancellationToken);
    }

    private async Task<GuildMemberCounts?> GetMemberCounts(Snowflake guildId, CancellationToken cancellationToken)
    {
        const int PAGE_SIZE = 1000;
        var total = 0;
        var bots = 0;
        Optional<Snowflake> after = default;

        while (true)
        {
            var page = await guildApi.ListGuildMembersAsync(guildId, PAGE_SIZE, after, cancellationToken);
            if (!page.IsSuccess || page.Entity.Any(member => !member.User.HasValue))
                return null;

            total += page.Entity.Count;
            bots += page.Entity.Count(member => member.User.Value.IsBot.HasValue && member.User.Value.IsBot.Value);

            if (page.Entity.Count < PAGE_SIZE)
                return new GuildMemberCounts(total, bots);

            var lastId = page.Entity.Max(member => member.User.Value.ID.Value);
            if (after.HasValue && lastId <= after.Value.Value)
                return null;

            after = new Snowflake(lastId);
        }
    }

    private static string FormatRoles(IGuild guild)
    {
        var roles = guild.Roles
            .Where(role => role.ID != guild.ID && (role.Colour.R != 0 || role.Colour.G != 0 || role.Colour.B != 0))
            .OrderByDescending(role => role.Position)
            .ThenByDescending(role => role.IsHoisted)
            .ThenBy(role => role.ID.Value)
            .ToArray();

        if (roles.Length == 0)
            return "No coloured roles.";

        var text = new StringBuilder();
        for (var index = 0; index < roles.Length; index++)
        {
            var mention = roles[index].ID.ToRoleMention();
            // Leave room for the suffix and keep mentions intact within Discord's 1024-character field limit.
            if (text.Length + mention.Length + 2 > 980)
                return $"{text} … and {roles.Length - index:N0} more";

            if (index > 0)
                text.Append(", ");

            text.Append(mention);
        }

        return text.ToString();
    }

    private sealed record GuildMemberCounts(int Total, int Bots);
}

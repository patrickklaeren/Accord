using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.UserMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RelayMessageUpdateDeleteToDiscordHandler(IDiscordRestChannelAPI channelApi, 
    IDiscordRestUserAPI userApi,
    IMediator mediator,
    ThumbnailHelper thumbnailHelper,
    JumpLinkHelper jumpLinkHelper) : 
    INotificationHandler<RelayMessageUpdateToDiscord>,
    INotificationHandler<RelayMessageDeleteToDiscord>
{
    private const int MAX_EMBED_FIELD_VALUE_LENGTH = 1024;
    private const string TRUNCATION_SUFFIX = "...";

    public async Task Handle(RelayMessageUpdateToDiscord request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.MessageLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.DiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;
        
        var fields = new List<EmbedField>
        {
            new("User", $"{DiscordFormatter.UserIdToMention(request.DiscordUserId)} ({request.DiscordUserId})", true),
            new("Message", jumpLinkHelper.FromIds(request.DiscordChannelId, request.DiscordMessageId), true),
            new("Old content", FormatContentForEmbed(request.OldContent)),
            new("New content", FormatContentForEmbed(request.NewContent))
        };

        var embed = new Embed(
            Title: $"✏️ {targetUser.Entity.Username} edited a message",
            Thumbnail: thumbnailHelper.GetAvatar(targetUser.Entity),
            Fields: fields,
            Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"));

        await PostEmbedToChannels(embed, channelsToPostTo, cancellationToken);
    }

    public async Task Handle(RelayMessageDeleteToDiscord request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.MessageLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.DiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;
        
        var fields = new List<EmbedField>
        {
            new("User", $"{DiscordFormatter.UserIdToMention(request.DiscordUserId)} ({request.DiscordUserId})", true),
            new("Channel", DiscordFormatter.ChannelIdToMention(request.DiscordChannelId), true),
            new("Deleted content", FormatContentForEmbed(request.Content))
        };

        if (!string.IsNullOrWhiteSpace(request.AttachmentsDetail))
            fields.Add(new EmbedField("Attachments", FormatContentForEmbed(request.AttachmentsDetail)));

        var embed = new Embed(
            Title: $"🗑️ {targetUser.Entity.Username} deleted a message",
            Thumbnail: thumbnailHelper.GetAvatar(targetUser.Entity),
            Fields: fields,
            Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"));

        await PostEmbedToChannels(embed, channelsToPostTo, cancellationToken);
    }

    private async Task PostEmbedToChannels(Embed embed,
        IReadOnlyCollection<ulong> channelIds,
        CancellationToken cancellationToken)
    {
        foreach (var channelId in channelIds)
        {
            await channelApi.CreateMessageAsync(new Snowflake(channelId),
                embeds: new[] { embed },
                ct: cancellationToken);
        }
    }

    private static string FormatContentForEmbed(string? content)
    {
        var value = content ?? "N/A";

        if (value.Length > MAX_EMBED_FIELD_VALUE_LENGTH)
            value = value[..(MAX_EMBED_FIELD_VALUE_LENGTH - TRUNCATION_SUFFIX.Length)] + TRUNCATION_SUFFIX;

        return value;
    }
}

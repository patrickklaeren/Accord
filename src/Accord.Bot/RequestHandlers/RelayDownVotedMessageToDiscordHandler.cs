using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.DemocraticDownVoting;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RelayDownVotedMessageToDiscordHandler(
    IDiscordRestChannelAPI channelApi,
    IMediator mediator)
    : IRequestHandler<RelayDownVotedMessageToDiscordRequest>
{
    public async Task Handle(RelayDownVotedMessageToDiscordRequest notification, CancellationToken cancellationToken)
    {
        var channelSnowflake = new Snowflake(notification.DownVotedMessageChannelId);
        var messageSnowflake = new Snowflake(notification.DownVotedMessageId);

        var logChannels = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.MessageLogs), cancellationToken);
        IMessage? originalMessage = null;

        if (logChannels.Count > 0)
        {
            var excludedChannels = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.DoNotLogMessages), cancellationToken);

            if (excludedChannels.Contains(notification.DownVotedMessageChannelId))
            {
                logChannels.Clear();
            }
            else
            {
                var messageResponse = await channelApi.GetChannelMessageAsync(channelSnowflake, messageSnowflake, cancellationToken);
                if (messageResponse.IsSuccess)
                    originalMessage = messageResponse.Entity;
            }
        }

        var downvotedBy = string.Join(", ", notification.DownVotedByUserIds.Select(DiscordFormatter.UserIdToMention));

        var deleteResponse = await channelApi
            .DeleteMessageAsync(channelSnowflake,
                messageSnowflake,
                reason: FormatContent($"Down voted by {downvotedBy}", 512),
                ct: cancellationToken);

        if (!deleteResponse.IsSuccess)
            return;
        
        var authorMention = DiscordFormatter.UserIdToMention(notification.DownVotedMessageAuthorId);
        var smallMarkdown = DiscordFormatter.ToSmallMarkdown("If you think this is not right, ping a moderator.");
        await channelApi.CreateMessageAsync(channelSnowflake, $"{authorMention} your message was downvoted to be removed.{Environment.NewLine}{smallMarkdown}", ct: cancellationToken);

        if (logChannels.Count == 0)
            return;

        var embeds = CreateLogEmbeds(notification, originalMessage);

        foreach (var logChannelId in logChannels)
        {
            foreach (var embed in embeds)
            {
                await channelApi.CreateMessageAsync(new Snowflake(logChannelId),
                    embeds: new[] { embed },
                    allowedMentions: new AllowedMentions(Parse: Array.Empty<MentionType>()),
                    ct: cancellationToken);
            }
        }
    }

    private static IReadOnlyList<Embed> CreateLogEmbeds(RelayDownVotedMessageToDiscordRequest request, IMessage? message)
    {
        const int VOTERS_PER_FIELD = 40;
        var voterPages = request.DownVotedByUserIds.Chunk(VOTERS_PER_FIELD).DefaultIfEmpty([]).ToList();
        var embeds = new List<Embed>(voterPages.Count);

        foreach (var voters in voterPages)
        {
            var fields = new List<EmbedField>
            {
                new("Author", $"{DiscordFormatter.UserIdToMention(request.DownVotedMessageAuthorId)} ({request.DownVotedMessageAuthorId})", true),
                new("Channel", DiscordFormatter.ChannelIdToMention(request.DownVotedMessageChannelId), true),
                new("Message ID", request.DownVotedMessageId.ToString(), true),
                new($"Downvoted by ({request.DownVotedByUserIds.Count})",
                    FormatContent(string.Join(", ", voters.Select(DiscordFormatter.UserIdToMention)), 1024))
            };

            if (message is { Attachments.Count: > 0 })
            {
                fields.Add(new EmbedField("Attachments", FormatContent(string.Join(", ", message.Attachments.Select(x => x.Filename)), 512)));
            }

            embeds.Add(new Embed(
                Title: "👎 Message democratically downvoted",
                Description: FormatContent(message is null ? "Original message content unavailable." : message.Content, 4096),
                Fields: fields,
                Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}")));
        }

        return embeds;
    }

    private static string FormatContent(string? content, int maxLength)
    {
        const string TRUNCATION_SUFFIX = "...";
        var value = string.IsNullOrWhiteSpace(content) ? "N/A" : content;

        return value.Length > maxLength
            ? value[..(maxLength - TRUNCATION_SUFFIX.Length)] + TRUNCATION_SUFFIX
            : value;
    }
}

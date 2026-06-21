using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Responders;

public partial class LinkedMessageResponder(
    JumpLinkHelper jumpLinkHelper,
    DiscordConfiguration discordConfiguration,
    ThumbnailHelper thumbnailHelper,
    IDiscordRestChannelAPI channelApi,
    IMediator mediator) : IResponder<IMessageCreate>
{
    [GeneratedRegex(
        @"^(?<Prelink>[\s\S]*?)?(?<OpenBrace><)?https?://(?:(?:ptb|canary)\.)?discord(app)?\.com/channels/(?<GuildId>\d+)/(?<ChannelId>\d+)/(?<MessageId>\d+)/?(?<CloseBrace>>)?(?<Postlink>[\s\S]*)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private partial Regex DiscordMessageLinkRegex();

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
            return Result.FromSuccess();

        var matches = DiscordMessageLinkRegex().Matches(gatewayEvent.Content);

        foreach (Match match in matches)
        {
            // check if the link is surrounded with < and >. This was too annoying to do in regex
            if (match.Groups["OpenBrace"].Success && match.Groups["CloseBrace"].Success)
                continue;

            if (!ulong.TryParse(match.Groups["GuildId"].Value, out var guildId)
                || !ulong.TryParse(match.Groups["ChannelId"].Value, out var channelId)
                || !ulong.TryParse(match.Groups["MessageId"].Value, out var messageId))
            {
                continue;
            }
            
            if (guildId != discordConfiguration.GuildId)
                continue;

            try
            {

                var channelSnowflake = new Snowflake(channelId);

                var channel = await channelApi.GetChannelAsync(channelSnowflake, ct);

                if (!channel.IsSuccess)
                    continue;

                if (channel.Entity.IsNsfw.Value)
                    continue;

                var messageSnowflake = new Snowflake(messageId);

                var message = await channelApi.GetChannelMessageAsync(channelSnowflake, messageSnowflake, ct);

                if (!message.IsSuccess)
                    continue;

                // If the message we downloaded has an embed with "Quoted by" in it, it's
                // likely the user has linked a quote in itself, so skip this message
                if (message.Entity.Embeds.Any(d => d.Fields.HasValue && d.Fields.Value.Any(c => c.Name == "Quoted by")))
                    continue;

                var embed = BuildEmbed(channel.Entity, message.Entity, gatewayEvent.Author);

                var workingMessageResult = await channelApi.CreateMessageAsync(
                    gatewayEvent.ChannelID,
                    embeds: new[] { embed },
                    allowedMentions: new AllowedMentions(MentionRepliedUser: false),
                    ct: ct);

                if (!workingMessageResult.IsSuccess)
                    continue;

                await mediator.Publish(new AddUserBotMessageRequest(workingMessageResult.Entity.ID.Value,
                        workingMessageResult.Entity.ChannelID.Value,
                        gatewayEvent.Author.ID.Value),
                    ct);

                if (string.IsNullOrEmpty(match.Groups["Prelink"].Value)
                    && string.IsNullOrEmpty(match.Groups["Postlink"].Value))
                {
                    await channelApi.DeleteMessageAsync(gatewayEvent.ChannelID, gatewayEvent.ID, ct: ct);
                }
            }
            catch
            {
                // Do nothing
            }
        }

        return Result.FromSuccess();
    }

    private Embed BuildEmbed(IChannel channel, IMessage message, IUser executingUser)
    {
        var richEmbed = TryBuildEmbedFromRichEmbed(channel, message, executingUser);
        
        if(richEmbed is not null)
            return richEmbed;

        var builder = new EmbedBuilder();
        string? imageUrl = null;

        const string DISCORD_SPOILER_FILE_NAME = "SPOILER_";
        if (message.Attachments.Any(x => x.Filename.StartsWith(DISCORD_SPOILER_FILE_NAME))
            || message.Embeds.Any() && DiscordFormatter.ContainsSpoiler(message.Content))
        {
            builder.AddField(new EmbedField("Spoiler warning", "The quoted message contains spoilered content."));
        }
        else if(!TryGetAttachmentUrl(message, out imageUrl))
        {
            if (!TryGetImageUrl(message, out imageUrl))
            {
                TryGetFallbackFileUrl(message, out imageUrl);
            }
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            builder.WithImageUrl(imageUrl);
        }

        if (message.Embeds.Any())
        {
            var firstEmbed = message.Embeds.First();
            builder.AddField(new EmbedField("Embed Type", firstEmbed.Type.Value.ToString()));
        }

        if (message.Activity.HasValue)
        {
            builder.AddField(new EmbedField("Invite Type", message.Activity.Value.Type.ToString()));
            builder.AddField(new EmbedField("Party Id", message.Activity.Value.PartyID.Value));
        }
        
        var jumpLink = jumpLinkHelper.FromMessage(message);
        var markdownLink = $"#{channel.Name} [(click here)]({jumpLink})";
        builder.AddField(new EmbedField("Quoted by", $"{executingUser.ID.ToUserMention()} from **{markdownLink}**"));

        var originalAuthorAvatar = thumbnailHelper.GetAvatar(message.Author);

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            builder.WithDescription(message.Content);
        }

        builder
            .WithAuthor(message.Author.Username, iconUrl: originalAuthorAvatar.Url)
            .WithTimestamp(message.Timestamp);

        var embed = builder.Build();
        return embed.Entity;
    }

    private Embed? TryBuildEmbedFromRichEmbed(IChannel channel, IMessage message, IUser executingUser)
    {
        var firstEmbed = message.Embeds.FirstOrDefault();

        if (firstEmbed?.Type != EmbedType.Rich)
            return null;

        var fields = new List<IEmbedField>();

        if (firstEmbed.Fields.HasValue)
        {
            fields.AddRange(firstEmbed.Fields.Value.ToList());    
        }

        var jumpLink = jumpLinkHelper.FromMessage(message);
        var markdownLink = $"#{channel.Name} [(click here)]({jumpLink})";
        fields.Add(new EmbedField("Quoted by", $"{executingUser.ID.ToUserMention()} from **{markdownLink}**"));

        return new Embed(Title: firstEmbed.Title, 
            Description: firstEmbed.Description, 
            Image: firstEmbed.Image,
            Footer: firstEmbed.Footer,
            Type: firstEmbed.Type,
            Url: firstEmbed.Url,
            Timestamp: firstEmbed.Timestamp,
            Colour: firstEmbed.Colour,
            Thumbnail: firstEmbed.Thumbnail,
            Video: firstEmbed.Video,
            Provider: firstEmbed.Provider,
            Author: firstEmbed.Author,
            Fields: fields);
    }
    
    private static bool TryGetAttachmentUrl(IMessage message, out string? url)
    {
        var firstAttachment = message.Attachments.FirstOrDefault();
        url = firstAttachment?.Height is null ? null : firstAttachment.Url;
        return url is not null;
    }
    
    private static bool TryGetImageUrl(IMessage message, out string? url)
    {
        var firstImage = message
            .Attachments
            .FirstOrDefault(x => x.ContentType.Value.Contains("image", StringComparison.OrdinalIgnoreCase));
        
        url = firstImage?.Url;
        return url is not null;
    }
    
    private static void TryGetFallbackFileUrl(IMessage message, out string? url)
    {
        var firstAttachment = message
            .Attachments
            .FirstOrDefault();
        
        url = firstAttachment?.Url;
    }
}
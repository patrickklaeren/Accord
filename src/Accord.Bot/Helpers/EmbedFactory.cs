using System;
using System.Collections.Generic;
using System.Linq;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class EmbedFactory(ThumbnailHelper thumbnailHelper)
{
    public EmbedBuilder FromMessage(IMessage message)
    {
        var richEmbed = TryBuildEmbedFromRichEmbed(message);

        if (richEmbed is not null)
            return EmbedBuilder.FromEmbed(richEmbed);

        var builder = new EmbedBuilder();
        string? imageUrl = null;

        const string DISCORD_SPOILER_FILE_NAME = "SPOILER_";
        if (message.Attachments.Any(x => x.Filename.StartsWith(DISCORD_SPOILER_FILE_NAME))
            || message.Embeds.Any() && DiscordFormatter.ContainsSpoiler(message.Content))
        {
            builder.AddField(new EmbedField("Spoiler warning", "The quoted message contains spoilered content."));
        }
        else if (!TryGetAttachmentUrl(message, out imageUrl))
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

        var originalAuthorAvatar = thumbnailHelper.GetAvatar(message.Author);

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            builder.WithDescription(message.Content);
        }

        return builder
            .WithAuthor(message.Author.Username, iconUrl: originalAuthorAvatar.Url)
            .WithTimestamp(message.Timestamp);
    }

    private Embed? TryBuildEmbedFromRichEmbed(IMessage message)
    {
        var firstEmbed = message.Embeds.FirstOrDefault();

        if (firstEmbed?.Type != EmbedType.Rich)
            return null;

        var fields = new List<IEmbedField>();

        if (firstEmbed.Fields.HasValue)
        {
            fields.AddRange(firstEmbed.Fields.Value.ToList());
        }

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
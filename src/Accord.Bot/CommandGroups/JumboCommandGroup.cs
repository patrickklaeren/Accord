using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Accord.Services;
using Microsoft.Extensions.Logging;
using OneOf;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public sealed partial class JumboCommandGroup(
    IHttpClientFactory httpClientFactory,
    DiscordConfiguration configuration,
    FeedbackService feedbackService,
    ILogger<JumboCommandGroup> logger) : AccordCommandGroup
{
    [Command("jumbo"), Description("Show a full-size image of an emoji")]
    public async Task<IResult> Jumbo([Description("The custom or Unicode emoji to enlarge")] string emoji)
    {
        var imageUrl = GetEmojiUrl(emoji.Trim());
        if (imageUrl is null)
            return await feedbackService.SendContextualAsync("I don't recognise that emoji. Please provide a single custom or Unicode emoji.", ct: CancellationToken);

        try
        {
            using var client = httpClientFactory.CreateClient();
            await using var image = await client.GetStreamAsync(imageUrl, CancellationToken);
            var filename = Path.GetFileName(new Uri(imageUrl).AbsolutePath);
            var options = new FeedbackMessageOptions(
                Attachments: new OneOf<FileData, IPartialAttachment>[] { new FileData(filename, image) });

            return await feedbackService.SendContextualAsync(options: options, ct: CancellationToken);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to download an emoji image for /jumbo");
            return await feedbackService.SendContextualAsync("I couldn't retrieve that emoji. It may no longer be available.", ct: CancellationToken);
        }
        catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
        {
            return await feedbackService.SendContextualAsync("Downloading that emoji timed out. Please try again.", ct: CancellationToken);
        }
    }

    private string? GetEmojiUrl(string emoji)
    {
        var customEmoji = CustomEmojiRegex().Match(emoji);
        if (customEmoji.Success)
        {
            if (!ulong.TryParse(customEmoji.Groups["id"].Value, out var id) || id == 0)
                return null;

            var extension = customEmoji.Groups["animated"].Value == "a" ? "gif" : "png";
            return $"{configuration.CdnBaseUrl.TrimEnd('/')}/emojis/{id}.{extension}?size=256";
        }

        // Keep flags, skin tones, keycaps, and joined emoji together as one text element.
        // The image lookup rejects text elements which aren't actual emoji.
        if (StringInfo.ParseCombiningCharacters(emoji).Length != 1 || emoji.All(char.IsAscii))
            return null;

        // Twemoji omits presentation selectors from unjoined emoji filenames.
        var isJoined = emoji.Contains('\u200D');
        var codepoints = emoji.EnumerateRunes()
            .Where(rune => rune.Value != 0xFE0E && (isJoined || rune.Value != 0xFE0F))
            .Select(rune => rune.Value.ToString("x", CultureInfo.InvariantCulture));

        return $"https://raw.githubusercontent.com/twitter/twemoji/master/assets/72x72/{string.Join('-', codepoints)}.png";
    }

    [GeneratedRegex(@"\A<(?<animated>a?):[A-Za-z0-9_]{2,32}:(?<id>[0-9]{1,20})>\z")]
    private static partial Regex CustomEmojiRegex();
}

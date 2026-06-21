using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.LinkPreviews;
using Accord.Services.Shlink;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class LinkShortnerCommandGroup(
    ICommandContext commandContext,
    PermissionUserFactory permissionUserFactory,
    FeedbackService feedbackService,
    IMediator mediator)
    : AccordCommandGroup
{
    [Command("shorten"), Description("Shorten a URL")]
    public async Task<IResult> Shorten([Greedy] string url)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            url = $"https://{url}";
        }
        
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            return await feedbackService.SendContextualAsync("Check your URL and try again?");
        }
        
        var user = await commandContext.ToPermissionUser(permissionUserFactory);
        var response = await mediator.Send(new ShortenUrlRequest(user, url.ToString()));

        if (!response.Success)
        {
            return await feedbackService.SendContextualAsync(response.ErrorMessage);
        }

        return await HandleSuccess(uri, response.Value!);
    }

    private async Task<IResult> HandleSuccess(Uri originalUrl, string shortenedUrl)
    {
        var preview = originalUrl.Host switch
        {
            "lab.razor.fyi" => await mediator.Send(new PreviewLabRazorFyiRequest(originalUrl)),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(preview))
        {
            return await feedbackService.SendContextualAsync($"Here is your shortened URL: {shortenedUrl}");
        }

        EmbedField[] fields = [new("Shortened URL", shortenedUrl)];

        var embed = new Embed(Title: "Shortened preview",
            Colour: Color.Green,
            Description: DiscordFormatter.FormatAsCodeBlock(DiscordFormatter.TruncateToEmbedField(preview)),
            Fields: fields);

        return await feedbackService.SendContextualAsync(embeds:  new[] { embed });
    }
}
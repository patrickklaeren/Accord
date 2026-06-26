using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Services.Changelog;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ChangelogCommandGroup(FeedbackService feedbackService, IMediator mediator) 
    : AccordCommandGroup
{
    [Command("changelog")]
    [Description("Shows the latest release notes from the Accord GitHub repository")]
    public async Task<IResult> Latest()
    {
        var response = await mediator.Send(new GetChangelogRequest());

        if (response.Failure)
        {
            return await feedbackService.SendContextualAsync(response.ErrorMessage);
        }

        var changelog = response.Value!;

        var description = changelog.Body.Length > 4000
            ? changelog.Body[..4000] + "..."
            : changelog.Body;

        var embed = new Embed(
            Title: $"Accord {changelog.Name}",
            Description: description,
            Url: changelog.Url,
            Timestamp: changelog.PublishedAt
        );

        return await feedbackService.SendContextualEmbedAsync(embed,
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions(Parse: new List<MentionType>())));
    }
}

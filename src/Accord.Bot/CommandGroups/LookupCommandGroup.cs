using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Diagnostics;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class LookupCommandGroup(IMediator mediator,
    ICommandContext commandContext,
    FeedbackService feedbackService) : AccordCommandGroup
{
    private const int PAGE_SIZE = 10;
    private const int MAX_RESULTS = 100;
    private const int LINE_MESSAGE_LIMIT = 100;

    [Command("lookup"), Description("Look up a C# diagnostic code")]
    public async Task<IResult> Lookup(
        [Description("Diagnostic code to look up")] string code)
    {
        var matches = await mediator.Send(new SearchDiagnosticsRequest(code));

        if (matches.Count == 0)
        {
            return await feedbackService.SendContextualAsync($"No diagnostic found matching `{code}`");
        }

        if (matches.Count == 1)
        {
            return await feedbackService.SendContextualEmbedAsync(BuildSingleEmbed(matches[0]));
        }

        var pages = BuildPages(matches);
        var proxy = commandContext.GetCommandProxy();

        return await feedbackService.SendContextualPaginatedMessageAsync(
            proxy.UserId,
            pages,
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions(Parse: new List<MentionType>())));
    }

    private static Embed BuildSingleEmbed(DiagnosticInfo diagnostic)
    {
        return new Embed(
            Title: diagnostic.Code,
            Description: diagnostic.Message,
            Url: diagnostic.Url is { } url ? url : default(Optional<string>));
    }

    private static IReadOnlyList<Embed> BuildPages(IReadOnlyList<DiagnosticInfo> matches)
    {
        var shown = matches.Count > MAX_RESULTS
            ? matches.Take(MAX_RESULTS).ToList()
            : matches;

        var pageCount = (int)double.Ceiling(shown.Count / (double)PAGE_SIZE);
        var pages = new List<Embed>(pageCount);

        var cappedNote = matches.Count > MAX_RESULTS
            ? $" · showing first {MAX_RESULTS}"
            : string.Empty;

        for (var page = 0; page < pageCount; page++)
        {
            var description = new StringBuilder();

            foreach (var diagnostic in shown.Skip(page * PAGE_SIZE).Take(PAGE_SIZE))
            {
                var message = diagnostic.Message.Length > LINE_MESSAGE_LIMIT
                    ? diagnostic.Message[..LINE_MESSAGE_LIMIT] + "..."
                    : diagnostic.Message;

                if (diagnostic.Url is not null)
                {
                    description.AppendLine($"[`{diagnostic.Code}`]({diagnostic.Url}): {message}");
                }
                else
                {
                    description.AppendLine($"`{diagnostic.Code}`: {message}");
                }
            }

            pages.Add(new Embed(
                Description: description.ToString().TrimEnd(),
                Footer: new EmbedFooter($"Page {page + 1} of {pageCount} · {matches.Count} matches{cappedNote}")));
        }

        return pages;
    }
}

using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Autocomplete;
using Accord.Services.Diagnostics;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class LookupCommandGroup(IMediator mediator,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("lookup"), Description("Look up a C# diagnostic code")]
    public async Task<IResult> Lookup(
        [Description("Diagnostic code to look up"), AutocompleteProvider(DiagnosticAutocompleteProvider.IDENTITY)] string code)
    {
        var matches = await mediator.Send(new SearchDiagnosticsRequest(code));

        if (matches.Count == 0)
        {
            return await feedbackService.SendContextualAsync($"No diagnostic found matching `{code}`");
        }

        if (matches.Count > 1)
        {
            return await feedbackService.SendContextualAsync(
                $"Multiple diagnostics match `{code}` — start typing and pick a suggestion");
        }

        return await feedbackService.SendContextualEmbedAsync(BuildEmbed(matches[0]));
    }

    private static Embed BuildEmbed(DiagnosticInfo diagnostic)
    {
        return new Embed(
            Title: diagnostic.Code,
            Description: diagnostic.Message,
            Url: diagnostic.Url is { } url ? url : default(Optional<string>));
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Diagnostics;
using Humanizer;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;

namespace Accord.Bot.Autocomplete;

public class DiagnosticAutocompleteProvider(IMediator mediator) : IAutocompleteProvider
{
    public const string IDENTITY = "autocomplete::diagnostic";

    private const int MAX_SUGGESTIONS = 25;
    private const int CHOICE_NAME_LIMIT = 100;

    public string Identity => IDENTITY;

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync(
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default)
    {
        var matches = await mediator.Send(new SuggestDiagnosticsRequest(userInput, MAX_SUGGESTIONS), ct);

        return matches
            .Select(diagnostic => (IApplicationCommandOptionChoice)new ApplicationCommandOptionChoice(
                Name: $"{diagnostic.Code}: {diagnostic.Message}".Truncate(CHOICE_NAME_LIMIT),
                Value: diagnostic.Code))
            .ToList();
    }
}
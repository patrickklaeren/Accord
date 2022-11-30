using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.NamePatterns;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("name-pattern"), AutoConstructor]
public partial class NamePatternCommandGroup: AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IMediator _mediator;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly CommandResponder _commandResponder;

    [Command("list"), Description("List all name patterns")]
    public async Task<IResult> List()
    {
        var user = await _commandContext.ToPermissionUser(_guildApi);

        var response = await _mediator.Send(new GetNamePatternsRequest());

        var blocked = response.Any(x => x.Type == PatternType.Blocked)
            ? string.Join(Environment.NewLine, response.Where(x => x.Type == PatternType.Blocked).Select(x => $"- `{x.Pattern}` [{x.OnDiscovery}]"))
            : "There are no blocked patterns";

        var allowed = response.Any(x => x.Type == PatternType.Allowed)
            ? string.Join(Environment.NewLine, response.Where(x => x.Type == PatternType.Allowed).Select(x => $"- `{x.Pattern}`"))
            : "There are no allowed patterns";

        var embed = new Embed(Title: "Name patterns",
            Description: "Allowed patterns supersede those that are blocked.",
            Fields: new EmbedField[]
            {
                new("Blocked", blocked),
                new("Allowed", allowed),
            });

        await _commandResponder.Respond(embed);

        return Result.FromSuccess();
    }

    [Command("allow"), Description("Add name pattern to allow")]
    public async Task<IResult> AllowPattern(string pattern)
    {
        var user = await _commandContext.ToPermissionUser(_guildApi);

        var response = await _mediator.Send(new AddNamePatternRequest(user, pattern, PatternType.Allowed, OnNamePatternDiscovery.DoNothing));

        await response.GetAction(async () => await _commandResponder.Respond($"{pattern} Allowed"),
            async () => await _commandResponder.Respond(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("block"), Description("Add name pattern to block")]
    public async Task<IResult> BlockPattern(string pattern, string onDiscovery)
    {
        var isParsedOnDiscovery = Enum.TryParse<OnNamePatternDiscovery>(onDiscovery, out var actualOnDiscovery);

        if (!isParsedOnDiscovery || !Enum.IsDefined(actualOnDiscovery))
        {
            await _commandResponder.Respond("Pattern discovery is not found");
        }
        else
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var response = await _mediator.Send(new AddNamePatternRequest(user, pattern, PatternType.Blocked, actualOnDiscovery));

            await response.GetAction(async () => await _commandResponder.Respond($"{pattern} Blocked, will {actualOnDiscovery}"),
                async () => await _commandResponder.Respond(response.ErrorMessage));
        }

        return Result.FromSuccess();
    }

    [Command("remove"), Description("Remove name pattern")]
    public async Task<IResult> RemovePattern(string pattern)
    {
        var user = await _commandContext.ToPermissionUser(_guildApi);

        var response = await _mediator.Send(new DeleteNamePatternRequest(user, pattern));

        await response.GetAction(async () => await _commandResponder.Respond($"{pattern} removed"),
            async () => await _commandResponder.Respond(response.ErrorMessage));

        return Result.FromSuccess();
    }
}
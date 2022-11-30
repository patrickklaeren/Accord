using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.RunOptions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[AutoConstructor]
public partial class RunOptionCommandGroup: AccordCommandGroup
{
    private readonly IMediator _mediator;
    private readonly CommandResponder _commandResponder;

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("configure"), Description("Configure an option for the bot")]
    public async Task<IResult> Configure(string type, string value)
    {
        if (!Enum.TryParse<RunOptionType>(type, out var actualRunOptionType) || !Enum.IsDefined(actualRunOptionType))
        {
            await _commandResponder.Respond("Configuration is not found");
        }
        else
        {
            var response = await _mediator.Send(new UpdateRunOptionRequest(actualRunOptionType, value));

            if (response.Success)
            {
                await _commandResponder.Respond($"{actualRunOptionType} configuration updated to {value}");
            }
            else
            {
                await _commandResponder.Respond($"{response.ErrorMessage}");
            }
        }

        return Result.FromSuccess();
    }
}
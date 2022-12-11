using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.RunOptions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[AutoConstructor]
public partial class RunOptionCommandGroup: AccordCommandGroup
{
    private readonly IMediator _mediator;
    private readonly FeedbackService _feedbackService;

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("configure"), Description("Configure an option for the bot"), Ephemeral]
    public async Task<IResult> Configure(string type, string value)
    {
        if (!Enum.TryParse<RunOptionType>(type, out var actualRunOptionType) || !Enum.IsDefined(actualRunOptionType))
        {
            return await _feedbackService.SendContextualAsync("Configuration is not found");
        }

        var response = await _mediator.Send(new UpdateRunOptionRequest(actualRunOptionType, value));

        if (response.Success)
        {
            return await _feedbackService.SendContextualAsync($"{actualRunOptionType} configuration updated to {value}");
        }

        return await _feedbackService.SendContextualAsync(response.ErrorMessage);
    }
}
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

public class RunOptionCommandGroup(IMediator mediator, FeedbackService feedbackService) : AccordCommandGroup
{
    [RequireDiscordPermission(DiscordPermission.Administrator), Command("configure"), Description("Configure an option for the bot"), Ephemeral]
    public async Task<IResult> Configure(RunOptionKey type, string value)
    {
        var response = await mediator.Send(new UpdateRunOptionRequest(type, value));

        if (response.Success)
        {
            return await feedbackService.SendContextualAsync($"{type} configuration updated to {value}");
        }

        return await feedbackService.SendContextualAsync(response.ErrorMessage);
    }
}
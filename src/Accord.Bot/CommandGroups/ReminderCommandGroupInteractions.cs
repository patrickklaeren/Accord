using System;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Services.Reminder;
using MediatR;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[AutoConstructor]
public partial class ReminderCommandGroupInteractions : InteractionGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly ICommandContext _commandContext;
    private readonly IMediator _mediator;

    [Modal("add-reminder-modal")]
    [Ephemeral]
    public async Task<Result> OnAddReminder(string description, string number, string periodUnit)
    {
        if (!int.TryParse(number, out var actualNumber))
        {
            return (Result)await _feedbackService.SendContextualAsync("The number you input was not a number? :S");
        }

        if (!_commandContext.TryGetUserID(out var userId))
        {
            return (Result)await _feedbackService.SendContextualAsync("Could not find your user ID");
        }

        if (!_commandContext.TryGetChannelID(out var channelId))
        {
            return (Result)await _feedbackService.SendContextualAsync("This channel looks odd - there is no ID");
        }

        var sanitizedMessage = description.DiscordSanitize();

        const int DAYS_IN_WEEK = 7;
        const int DAYS_IN_YEAR = 365;

        var timeSpan = periodUnit switch
        {
            "Seconds" => new TimeSpan(0, 0, 0, actualNumber),
            "Minutes" => new TimeSpan(0, 0, actualNumber, 0),
            "Hours" => new TimeSpan(0, actualNumber, 0, 0),
            "Days" => new TimeSpan(actualNumber, 0, 0, 0),
            "Weeks" => new TimeSpan(actualNumber * DAYS_IN_WEEK, 0, 0, 0),
            "Years" => new TimeSpan(actualNumber * DAYS_IN_YEAR, 0, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(periodUnit), periodUnit, "Unknwon time unit")
        };

        await _mediator.Send(new AddReminderRequest(
            userId.Value,
            channelId.Value,
            timeSpan,
            sanitizedMessage
        ));

        return (Result)await _feedbackService.SendContextualAsync("I'll remind you!");
    }
}
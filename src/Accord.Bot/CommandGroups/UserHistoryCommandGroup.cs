using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Services.UserHistories;
using Humanizer;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class UserHistoryCommandGroup(ICommandContext commandContext, 
    IMediator mediator, 
    FeedbackService feedbackService) 
    : AccordCommandGroup
{
    [Command("note"), Description("Add a note to a user")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> AddNote(IGuildMember member, string content)
    {
        commandContext.TryGetUserID(out var userId);

        var sanitized = content.SanitiseDiscordContent();

        var response = await mediator.Send(new AddUserHistoryRequest(
            member.User.Value!.ID.Value,
            userId.Value,
            sanitized
        ));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Note added to {member.User.Value.Username}'s history"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }
}

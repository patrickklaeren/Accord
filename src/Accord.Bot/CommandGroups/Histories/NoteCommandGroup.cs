using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Domain.Model;
using Accord.Services.UserHistories;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups.Histories;

public class NoteCommandGroup(ICommandContext commandContext,
    IMediator mediator, 
    FeedbackService feedbackService) 
    : AccordCommandGroup
{
    [Command("note"), Description("Add a note to a user")]
    public async Task<IResult> AddNote(IGuildMember member, string content)
    {
        commandContext.TryGetUserID(out var userId);

        var sanitized = content.SanitiseDiscordContent();

        var response = await mediator.Send(new AddUserHistoryRequest(
            member.User.Value!.ID.Value,
            userId.Value,
            sanitized,
            UserHistoryType.Note
        ));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Note #{response.Value:0000} added to {member.User.Value.Username}'s history"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }
}

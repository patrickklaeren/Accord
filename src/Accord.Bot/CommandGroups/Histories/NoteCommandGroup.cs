using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.UserHistories;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups.Histories;

public class NoteCommandGroup(ICommandContext commandContext,
    IDiscordRestChannelAPI channelApi,
    IMediator mediator, 
    FeedbackService feedbackService) 
    : AccordCommandGroup
{
    [Command("note"), Description("Add a note to a user"), Ephemeral]
    public async Task<IResult> Note(IGuildMember member, string content)
    {
        commandContext.TryGetUserID(out var userId);

        var sanitized = content.SanitiseDiscordContent();

        var response = await mediator.Send(new AddUserHistoryRequest(
            member.User.Value!.ID.Value,
            userId.Value,
            sanitized,
            UserHistoryType.Note
        ));

        var targetUserMention = member.User.Value.ID.ToUserMention();
        
        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Note #{response.Value:0000} added to {targetUserMention}'s history"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }
    
    [Command("warn"), Description("Warn a user"), Ephemeral, RequireDiscordPermission(DiscordPermission.Administrator)]
    public async Task<IResult> Warn(IGuildMember member, string content, bool announce = true)
    {
        commandContext.TryGetUserID(out var userId);
        commandContext.TryGetChannelID(out var channelId);

        var sanitized = content.SanitiseDiscordContent();

        var response = await mediator.Send(new AddUserHistoryRequest(
            member.User.Value!.ID.Value,
            userId.Value,
            sanitized,
            UserHistoryType.Warning
        ));

        var targetUserMention = member.User.Value.ID.ToUserMention();

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Warning #{response.Value:0000} added to {targetUserMention}'s history"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        if (announce)
        {
            await channelApi.CreateMessageAsync(channelId, $"{targetUserMention} you have been warned. {content}");
        }

        return Result.FromSuccess();
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("unmute")]
public class UnmuteCommandGroup(ICommandContext commandContext,
    IDiscordRestGuildAPI guildApi,
    PermissionUserFactory permissionUserFactory,
    IMediator mediator, 
    FeedbackService feedbackService) 
    : AccordCommandGroup
{
    [Command("temp"), Description("Unmutes a temporarily muted user (from text and voice)"), Ephemeral]
    public async Task<IResult> TempMute(IUser user)
    {
        var proxy = commandContext.GetCommandProxy();
        var actingUser = await commandContext.ToPermissionUser(permissionUserFactory);

        var hasPermission = await mediator.Send(new UserHasPermissionRequest(actingUser,
            PermissionType.TemporaryMute));

        if (!hasPermission)
        {
            return await feedbackService.SendContextualAsync("Missing permission");
        }

        if (user.ID.Value == actingUser.DiscordUserId)
        {
            return await feedbackService.SendContextualAsync("Cannot unmute yourself");
        }
        
        var modifyReason = $"On behalf of {actingUser.DiscordUserId}";
        var response = await guildApi.ModifyGuildMemberAsync(proxy.GuildId,
            user.ID,
            communicationDisabledUntil: null,
            reason: modifyReason);

        if (!response.IsSuccess)
        {
            return await feedbackService.SendContextualAsync("Failed unmuting user");
        }

        var targetUserMention = user.ID.ToUserMention();

        return await feedbackService.SendContextualAsync($"{targetUserMention} unmuted",
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions(Parse: new List<MentionType>())));
    }
}

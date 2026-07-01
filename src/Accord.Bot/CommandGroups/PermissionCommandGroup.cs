using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("permission")]
public class PermissionCommandGroup(IMediator mediator, FeedbackService feedbackService) : AccordCommandGroup
{
    [RequireDiscordPermission(DiscordPermission.Administrator), Command("add-user"), Description("Add permission to a user"), Ephemeral]
    public async Task<IResult> AddPermissionToMember(IGuildMember member, PermissionType type)
    {
        await mediator.Send(new AddPermissionForUserRequest(member.User.Value.ID.Value, type));
        return await feedbackService.SendContextualAsync($"{type} permission added to {member.User.Value.ID.ToUserMention()}");
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("add-role"), Description("Add permission to a role"), Ephemeral]
    public async Task<IResult> AddPermissionToRole(IRole role, PermissionType type)
    {
        await mediator.Send(new AddPermissionForRoleRequest(role.ID.Value, type));
        return await feedbackService.SendContextualAsync($"{type} permission added to `{role.Name}`");
    }
}

using System;
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

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("adduser"), Description("Add permission to a user"), Ephemeral]
    public async Task<IResult> AddPermissionToMember(IGuildMember member, string type)
    {
        if (!Enum.TryParse<PermissionType>(type, out var actualPermission) || !Enum.IsDefined(actualPermission))
        {
            return await feedbackService.SendContextualAsync("Permission is not found");
        }

        if (!member.User.HasValue)
        {
            return Result.FromSuccess();
        }

        await mediator.Send(new AddPermissionForUserRequest(member.User.Value.ID.Value, actualPermission));
        return await feedbackService.SendContextualAsync($"{actualPermission} permission added to {member.User.Value.ID.ToUserMention()}");
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("addrole"), Description("Add permission to a role"), Ephemeral]
    public async Task<IResult> AddPermissionToRole(IRole role, string type)
    {
        if (!Enum.TryParse<PermissionType>(type, out var actualPermission) || !Enum.IsDefined(actualPermission))
        {
            return await feedbackService.SendContextualAsync("Permission is not found");
        }

        await mediator.Send(new AddPermissionForRoleRequest(role.ID.Value, actualPermission));
        return await feedbackService.SendContextualAsync($"{actualPermission} permission added to `{role.Name}`");
    }
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("permission")]
public class PermissionCommandGroup: AccordCommandGroup
{
    private readonly IMediator _mediator;
    private readonly CommandResponder _commandResponder;

    public PermissionCommandGroup(IMediator mediator,
        CommandResponder commandResponder)
    {
        _mediator = mediator;
        _commandResponder = commandResponder;
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("adduser"), Description("Add permission to a user")]
    public async Task<IResult> AddPermissionToMember(IGuildMember member, string type)
    {
        if (!Enum.TryParse<PermissionType>(type, out var actualPermission) || !Enum.IsDefined(actualPermission))
        {
            await _commandResponder.Respond("Permission is not found");
        }
        else if(member.User.HasValue)
        {
            await _mediator.Send(new AddPermissionForUserRequest(member.User.Value.ID.Value, actualPermission));
            await _commandResponder.Respond($"{actualPermission} permission added to {member.User.Value.ID.ToUserMention()}");
        }

        return Result.FromSuccess();
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("addrole"), Description("Add permission to a role")]
    public async Task<IResult> AddPermissionToRole(IRole role, string type)
    {
        if (!Enum.TryParse<PermissionType>(type, out var actualPermission) || !Enum.IsDefined(actualPermission))
        {
            await _commandResponder.Respond("Permission is not found");
        }
        else
        {
            await _mediator.Send(new AddPermissionForRoleRequest(role.ID.Value, actualPermission));
            await _commandResponder.Respond($"{actualPermission} permission added to `{role.Name}`");
        }

        return Result.FromSuccess();
    }
}
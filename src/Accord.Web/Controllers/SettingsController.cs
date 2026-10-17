using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
using Accord.Services.Guilds;
using Accord.Services.Permissions;
using Accord.Web.DiscordServices;
using Accord.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accord.Web.Controllers;

[Route("settings")]
public sealed class SettingsController(IMediator mediator, DiscordUserService currentUser) : AccordController(mediator, currentUser)
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var access = await Mediator.Send(new GetGuildSettingsAccessRequest(CurrentUser.GetDiscordId()), ct);
        if (!access.CanAccessSettings)
            return AccessDenied("You need Administrator access or the Manage Flags permission to manage settings.");

        return View(access);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken ct)
    {
        if (await GetPermissionUser(ct) is not { IsAdministrator: true })
            return AccessDenied("You need Administrator access in the guild to manage permissions.");

        var roles = await Mediator.Send(new GetGuildRolesRequest(), ct);
        var permissions = await Mediator.Send(new GetAllPermissionsRequest(), ct);
        var names = new Dictionary<ulong, string>();
        foreach (var id in permissions.UserPermissions.Select(permission => permission.UserId).Distinct())
        {
            var member = await Mediator.Send(new GetGuildMemberRequest(id), ct);
            names[id] = member.Success ? member.Value!.Nickname ?? member.Value.Username : id.ToString();
        }
        return View(new PermissionsViewModel(roles.Roles, permissions, names));
    }

    [HttpPost("permissions/role")]
    public async Task<IActionResult> RolePermission(PermissionInputModel input, CancellationToken ct)
    {
        if (await GetPermissionUser(ct) is not { IsAdministrator: true })
            return AccessDenied("You need Administrator access in the guild to manage permissions.");
        if (!ModelState.IsValid)
            return InvalidForm(nameof(Permissions));

        var roles = await Mediator.Send(new GetGuildRolesRequest(), ct);
        if (roles.Roles.All(role => role.Id != input.SubjectId))
            return Saved(ServiceResponse.Fail("This role no longer exists in the guild."), nameof(Permissions), string.Empty);

        if (input.Enabled)
            await Mediator.Send(new AddPermissionForRoleRequest(input.SubjectId, input.Permission), ct);
        else
            await Mediator.Send(new RemovePermissionForRoleRequest(input.SubjectId, input.Permission), ct);
        return Saved(ServiceResponse.Ok(), nameof(Permissions), "Role permission saved.");
    }

    [HttpPost("permissions/user")]
    public async Task<IActionResult> UserPermission(PermissionInputModel input, CancellationToken ct)
    {
        if (await GetPermissionUser(ct) is not { IsAdministrator: true })
            return AccessDenied("You need Administrator access in the guild to manage permissions.");
        if (!ModelState.IsValid)
            return InvalidForm(nameof(Permissions));

        if (input.Enabled)
        {
            var member = await Mediator.Send(new GetGuildMemberRequest(input.SubjectId), ct);
            if (!member.Success)
                return Saved(ServiceResponse.Fail("Enter the ID of a user in this guild."), nameof(Permissions), string.Empty);
            await Mediator.Send(new AddPermissionForUserRequest(input.SubjectId, input.Permission), ct);
        }
        else
            await Mediator.Send(new RemovePermissionForUserRequest(input.SubjectId, input.Permission), ct);
        return Saved(ServiceResponse.Ok(), nameof(Permissions), input.Enabled ? "User override added." : "User override removed.");
    }

    [HttpGet("channel-flags")]
    public async Task<IActionResult> ChannelFlags(ulong? channelId, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageFlags, ct))
            return AccessDenied("You need the Manage Flags permission to manage channel flags.");

        var flags = await Mediator.Send(new GetAllChannelFlagsRequest(), ct);
        var directory = await Mediator.Send(new GetGuildChannelsRequest(), ct);
        var ids = flags.Flags.Select(flag => flag.DiscordChannelId).ToHashSet();
        if (!ModelState.IsValid)
            TempData["Error"] = "Enter a valid Discord channel ID.";
        else if (channelId is > 0)
        {
            if (directory.Channels.Any(channel => channel.Id == channelId))
                ids.Add(channelId.Value);
            else
                TempData["Error"] = "That channel could not be found in this guild.";
        }

        var channels = ids.Select(id => directory.Channels.FirstOrDefault(channel => channel.Id == id)
            ?? new GuildChannelDto(id, id.ToString())).OrderBy(channel => channel.Name).ToArray();
        return View(new ChannelFlagsViewModel(channels, flags.Flags));
    }

    [HttpPost("channel-flags")]
    public async Task<IActionResult> ChannelFlag(ChannelFlagInputModel input, CancellationToken ct)
    {
        var user = await GetPermissionUser(ct);
        if (!await HasPermission(user, PermissionType.ManageFlags, ct))
            return AccessDenied("You need the Manage Flags permission to manage channel flags.");
        if (!ModelState.IsValid)
            return InvalidForm(nameof(ChannelFlags));

        // Permit removing flags from deleted channels, but only add flags to current guild channels.
        if (input.Enabled && (await Mediator.Send(new GetGuildChannelsRequest(), ct)).Channels.All(channel => channel.Id != input.ChannelId))
            return Saved(ServiceResponse.Fail("That channel could not be found in this guild."), nameof(ChannelFlags), string.Empty);

        var result = input.Enabled
            ? await Mediator.Send(new AddChannelFlagRequest(user!, input.Flag, input.ChannelId), ct)
            : await Mediator.Send(new DeleteChannelFlagRequest(user!, input.Flag, input.ChannelId), ct);
        return Saved(result, nameof(ChannelFlags), "Channel flag saved.", new { channelId = input.ChannelId });
    }
}

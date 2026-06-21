using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

[RegisterScoped]
public class UserPermissionService(AccordContext db)
{
    public async Task<bool> HasPermission(PermissionUser user,  PermissionType permission)
    {
        var permissions = await GetPermissionsForUser(user);
        return permissions.Any(ownedPermission => ownedPermission == permission);
    }

    public async Task AddPermissionToUser(ulong discordUserId, PermissionType permission, CancellationToken cancellationToken)
    {
        if (await db.UserPermissions.AnyAsync(x => x.UserId == discordUserId
                                                   && x.Type == permission, cancellationToken: cancellationToken))
        {
            return;
        }

        var permissionEntity = new UserPermission
        {
            UserId = discordUserId,
            Type = permission,
        };

        db.Add(permissionEntity);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPermissionToRole(ulong discordRoleId, PermissionType permission, CancellationToken cancellationToken)
    {
        if (await db.RolePermissions.AnyAsync(x => x.RoleId == discordRoleId
                                                   && x.Type == permission, cancellationToken: cancellationToken))
        {
            return;
        }

        var permissionEntity = new RolePermission
        {
            RoleId = discordRoleId,
            Type = permission,
        };

        db.Add(permissionEntity);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePermissionFromRole(ulong discordRoleId, PermissionType permission, CancellationToken ct)
    {
        var entity = await db.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == discordRoleId && x.Type == permission, ct);

        if (entity is null)
            return;

        db.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemovePermissionFromUser(ulong discordUserId, PermissionType permission, CancellationToken ct)
    {
        var entity = await db.UserPermissions
            .FirstOrDefaultAsync(x => x.UserId == discordUserId && x.Type == permission, ct);

        if (entity is null)
            return;

        db.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<PermissionType>> GetPermissionsForUser(PermissionUser user)
    {
        return await db.Permissions
            .Where(x => x is UserPermission && ((UserPermission)x).UserId == user.DiscordUserId
                        || x is RolePermission && user.OwnedDiscordRoleIds.Contains(((RolePermission)x).RoleId))
            .Select(x => x.Type)
            .ToListAsync();
    }
}
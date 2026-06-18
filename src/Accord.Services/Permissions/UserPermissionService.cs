using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

[RegisterScoped]
public class UserPermissionService(AccordContext db, IAppCache appCache)
{
    public async Task<bool> HasPermission(PermissionUser user,  PermissionType permission)
    {
        var permissions = await appCache.GetOrAddAsync(BuildGetPermissionsForUserCacheKey(user.DiscordUserId),
            () => GetPermissionsForUser(user),
            DateTimeOffset.UtcNow.AddMinutes(5));

        return permissions.Any(ownedPermission => ownedPermission == permission);
    }

    private void InvalidateCache(ulong discordUserId)
    {
        appCache.Remove(BuildGetPermissionsForUserCacheKey(discordUserId));
    }

    private static string BuildGetPermissionsForUserCacheKey(ulong discordUserId)
    {
        return $"{nameof(UserHasPermission)}/{nameof(GetPermissionsForUser)}/{discordUserId}";
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
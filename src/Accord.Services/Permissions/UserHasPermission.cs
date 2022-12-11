using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

public sealed record UserHasPermissionRequest(PermissionUser User, PermissionType Permission) : IRequest<bool>;
public sealed record PermissionsUpdateNotification(ulong UserId) : INotification;

[AutoConstructor]
public partial class UserHasPermission : NotificationHandler<PermissionsUpdateNotification>, IRequestHandler<UserHasPermissionRequest, bool>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<bool> Handle(UserHasPermissionRequest request, CancellationToken cancellationToken)
    {
        var permissions = await _appCache.GetOrAddAsync(BuildGetPermissionsForUserCacheKey(request.User.DiscordUserId),
            () => GetPermissionsForUser(request.User),
            DateTimeOffset.Now.AddMinutes(5));

        return permissions.Any(ownedPermission => ownedPermission == request.Permission);
    }

    protected override void Handle(PermissionsUpdateNotification request)
    {
        _appCache.Remove(BuildGetPermissionsForUserCacheKey(request.UserId));
    }

    private static string BuildGetPermissionsForUserCacheKey(ulong discordUserId)
    {
        return $"{nameof(UserHasPermission)}/{nameof(GetPermissionsForUser)}/{discordUserId}";
    }

    private async Task<List<PermissionType>> GetPermissionsForUser(PermissionUser user)
    {
        return await _db.Permissions
            .Where(x => x is UserPermission && ((UserPermission)x).UserId == user.DiscordUserId
                        || x is RolePermission && user.OwnedDiscordRoleIds.Contains(((RolePermission)x).RoleId))
            .Select(x => x.Type)
            .ToListAsync();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class PermissionService
    {
        private readonly AccordContext _db;
        private readonly IAppCache _appCache;

        public PermissionService(AccordContext db, IAppCache appCache)
        {
            _db = db;
            _appCache = appCache;
        }

        public async Task<bool> UserHasPermission(PermissionUser user, PermissionType permissionType)
        {
            var permissions = await GetPermissionsForUser(user);
            return permissions.Any(ownedPermission  => ownedPermission == permissionType);
        }

        public async Task<ServiceResponse> AddPermissionForUser(ulong discordUserId, PermissionType permissionType)
        {
            if (await _db.UserPermissions.AnyAsync(x => x.UserId == discordUserId
                                                     && x.Type == permissionType))
            {
                return ServiceResponse.Ok();
            }

            var permissionEntity = new UserPermission
            {
                UserId = discordUserId,
                Type = permissionType,
            };

            _db.Add(permissionEntity);

            await _db.SaveChangesAsync();

            _appCache.Remove(BuildGetPermissionsForUserCacheKey(discordUserId));

            return ServiceResponse.Ok();
        }

        public async Task<ServiceResponse> AddPermissionForRole(ulong discordRoleId, PermissionType permissionType)
        {
            if (await _db.RolePermissions.AnyAsync(x => x.RoleId == discordRoleId
                                                     && x.Type == permissionType))
            {
                return ServiceResponse.Ok();
            }

            var permissionEntity = new RolePermission
            {
                RoleId = discordRoleId,
                Type = permissionType,
            };

            _db.Add(permissionEntity);

            await _db.SaveChangesAsync();

            return ServiceResponse.Ok();
        }

        private static string BuildGetPermissionsForUserCacheKey(ulong discordUserId)
        {
            return $"{nameof(PermissionService)}/{nameof(GetPermissionsForUser)}/{discordUserId}";
        }

        private async Task<List<PermissionType>> GetPermissionsForUser(PermissionUser user)
        {
            return await _appCache.GetOrAddAsync(BuildGetPermissionsForUserCacheKey(user.DiscordUserId), 
                () => GetPermissionsForUserInternal(user), 
                DateTimeOffset.Now.AddMinutes(5));
        }

        private async Task<List<PermissionType>> GetPermissionsForUserInternal(PermissionUser user)
        {
            return await _db.Permissions
                .Where(x => x is UserPermission && ((UserPermission)x).UserId == user.DiscordUserId 
                            || x is RolePermission && user.OwnedDiscordRoleIds.Contains(((RolePermission)x).RoleId))
                .Select(x => x.Type)
                .ToListAsync();
        }
    }

    public record PermissionUser(ulong DiscordUserId, IEnumerable<ulong> OwnedDiscordRoleIds);
}
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

namespace Accord.Services.Permissions
{
    public sealed record UserIsExemptFromRaidRequest(ulong UserId) : IRequest<bool>;

    public class UserIsExemptFromRaid : NotificationHandler<PermissionsUpdateNotification>, IRequestHandler<UserIsExemptFromRaidRequest, bool>
    {
        private static readonly string AllowlistedUsersCacheKey = $"{nameof(UserIsExemptFromRaid)}/{nameof(GetAllowlistedUsers)}";
        private readonly AccordContext _db;
        private readonly IAppCache _appCache;

        public UserIsExemptFromRaid(AccordContext db, IAppCache appCache)
        {
            _db = db;
            _appCache = appCache;
        }

        public async Task<bool> Handle(UserIsExemptFromRaidRequest request, CancellationToken cancellationToken)
        {
            var allowedUsers = await _appCache.GetOrAddAsync(AllowlistedUsersCacheKey,
                () => GetAllowlistedUsers(),
                DateTimeOffset.Now.AddHours(1));

            return allowedUsers.Contains(request.UserId);
        }

        protected override void Handle(PermissionsUpdateNotification request)
        {
            _appCache.Remove(AllowlistedUsersCacheKey);
        }

        private async Task<List<ulong>> GetAllowlistedUsers()
        {
            return await _db.Permissions
                .OfType<UserPermission>()
                .Where(x => x.Type == PermissionType.BypassRaidCheck)
                .Select(x => x.UserId)
                .ToListAsync();
        }
    }
}
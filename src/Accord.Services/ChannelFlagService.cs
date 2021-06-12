using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class ChannelFlagService
    {
        private readonly AccordContext _db;
        private readonly PermissionService _permissionService;
        private readonly IAppCache _appCache;

        public ChannelFlagService(AccordContext db, IAppCache appCache, PermissionService permissionService)
        {
            _db = db;
            _appCache = appCache;
            _permissionService = permissionService;
        }

        private static string BuildIsChannelIgnoredFromXpCacheKey(ulong discordChannelId)
        {
            return $"{nameof(XpService)}/{nameof(IsChannelIgnoredFromXp)}/{discordChannelId}";
        }

        public async Task<bool> IsChannelIgnoredFromXp(ulong discordChannelId)
        {
            return await _appCache.GetOrAddAsync(BuildIsChannelIgnoredFromXpCacheKey(discordChannelId),
                () => IsChannelIgnoredFromXpInternal(discordChannelId),
                DateTimeOffset.Now.AddDays(30));
        }

        private async Task<bool> IsChannelIgnoredFromXpInternal(ulong discordChannelId)
        {
            return await _db.ChannelFlags
                .Where(x => x.DiscordChannelId == discordChannelId)
                .AnyAsync(x => x.Type == ChannelFlagType.IgnoredFromXp);
        }

        public async Task<ServiceResponse> AddFlag(PermissionUser user, ChannelFlagType channelFlag, ulong discordChannelId)
        {
            if (!await _permissionService.UserHasPermission(user, PermissionType.AddFlags))
            {
                return ServiceResponse.Fail("Missing permission");
            }

            if (await _db.ChannelFlags.AnyAsync(x => x.DiscordChannelId == discordChannelId 
                                                     && x.Type == channelFlag))
            {
                return ServiceResponse.Ok();
            }

            var entity = new ChannelFlag
            {
                DiscordChannelId = discordChannelId,
                Type = channelFlag,
            };

            _db.Add(entity);

            await _db.SaveChangesAsync();

            _appCache.Remove(BuildIsChannelIgnoredFromXpCacheKey(discordChannelId));

            return ServiceResponse.Ok();
        }
    }
}

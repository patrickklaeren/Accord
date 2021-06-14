using System;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class UserService
    {
        private readonly AccordContext _db;
        private readonly IAppCache _appCache;

        public UserService(AccordContext db, IAppCache appCache)
        {
            _db = db;
            _appCache = appCache;
        }

        public async Task EnsureUserExists(ulong discordUserId)
        {
            if (await DoesExist(discordUserId))
                return;

            var user = new User
            {
                Id = discordUserId,
                FirstSeenDateTime = DateTimeOffset.Now,
            };

            _db.Add(user);

            await _db.SaveChangesAsync();

            _appCache.Remove(BuildDoesExistKey(discordUserId));
        }

        private static string BuildDoesExistKey(ulong discordUserId)
        {
            return $"{nameof(UserService)}/{nameof(DoesExist)}/{discordUserId}";
        }

        private async Task<bool> DoesExist(ulong discordUserId)
        {
            return await _appCache
                .GetOrAddAsync(BuildDoesExistKey(discordUserId),
                    () => _db.Users.AnyAsync(x => x.Id == discordUserId),
                    DateTimeOffset.Now.AddDays(30));
        }
    }
}

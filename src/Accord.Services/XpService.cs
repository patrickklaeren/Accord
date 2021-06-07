using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class XpService
    {
        private readonly AccordContext _db;

        public XpService(AccordContext db)
        {
            _db = db;
        }

        public async Task<List<XpUser>> GetLeaderboard()
        {
            return await _db.Users
                .OrderByDescending(x => x.Xp)
                .ThenBy(x => x.LastSeenDateTime)
                .Take(10)
                .Select(x => new XpUser(x.Id, x.Xp))
                .ToListAsync();
        }
    }

    public record XpUser(ulong DiscordUserId, float Xp);
}

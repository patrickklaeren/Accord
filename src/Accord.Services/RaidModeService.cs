using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;

namespace Accord.Services
{
    public class RaidModeService
    {
        private readonly AccordContext _db;
        private readonly RaidCalculator _raidCalculator;
        private readonly IAppCache _appCache;

        public RaidModeService(AccordContext db, RaidCalculator raidCalculator, IAppCache appCache)
        {
            _db = db;
            _raidCalculator = raidCalculator;
            _appCache = appCache;
        }
    }
}

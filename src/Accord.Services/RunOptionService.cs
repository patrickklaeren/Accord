using System;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class RunOptionService
    {
        private readonly AccordContext _db;

        public RunOptionService(AccordContext db)
        {
            _db = db;
        }

        public async Task<ServiceResponse> Update(RunOptionType type, string rawValue)
        {
            var runOption = await _db.RunOptions
                .SingleAsync(x => x.Type == type);

            var success = false;

            switch (type)
            {
                case RunOptionType.RaidModeEnabled when bool.TryParse(rawValue, out var actualValue):
                    runOption.Value = actualValue.ToString();
                    success = true;
                    break;
                case RunOptionType.AutoRaidModeEnabled when bool.TryParse(rawValue, out var actualValue):
                    runOption.Value = actualValue.ToString();
                    success = true;
                    break;
                case RunOptionType.JoinsToTriggerRaidModePerMinute when int.TryParse(rawValue, out var actualValue):
                    runOption.Value = actualValue.ToString();
                    success = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (!success)
            {
                return ServiceResponse.Fail("Failed updating value");
            }

            await _db.SaveChangesAsync();

            return ServiceResponse.Ok();
        }
    }
}

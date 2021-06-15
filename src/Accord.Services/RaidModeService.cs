using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class RaidModeService
    {
        private readonly AccordContext _db;
        private readonly RaidCalculator _raidCalculator;
        private readonly IAppCache _appCache;
        private readonly IMediator _mediator;

        public RaidModeService(AccordContext db, RaidCalculator raidCalculator, IAppCache appCache, IMediator mediator)
        {
            _db = db;
            _raidCalculator = raidCalculator;
            _appCache = appCache;
            _mediator = mediator;
        }

        public async Task Process(DateTimeOffset joinedDateTime)
        {
            var limitPerOneMinute = await GetLimitPerOneMinute();
            var isRaid = _raidCalculator.CalculateIsRaid(joinedDateTime, limitPerOneMinute);
            var isAutoRaidModeEnabled = await GetIsAutoRaidModeEnabled();
            var isInExistingRaidMode = await GetIsInRaidMode();

            if (isRaid != isInExistingRaidMode)
            {
                var runOption = await _db.RunOptions
                    .SingleAsync(x => x.Type == RunOptionType.RaidModeEnabled);

                runOption.Value = isRaid.ToString();

                await _db.SaveChangesAsync();

                _appCache.Remove(BuildGetIsInRaidModeCacheKey());
            }

            await _mediator.Send(new RaidAlertRequest(isRaid, isInExistingRaidMode, isAutoRaidModeEnabled));
        }

        public static string BuildGetLimitPerOneMinuteCacheKey()
        {
            return $"{nameof(RaidModeService)}/{nameof(GetLimitPerOneMinute)}";
        }

        private async Task<int> GetLimitPerOneMinute()
        {
            return await _appCache.GetOrAddAsync(BuildGetLimitPerOneMinuteCacheKey(), 
                GetLimitPerOneMinuteInternal, 
                DateTimeOffset.Now.AddDays(30));
        }

        private async Task<int> GetLimitPerOneMinuteInternal()
        {
            var value = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.JoinsToTriggerRaidModePerMinute)
                .Select(x => x.Value)
                .SingleAsync();

            return int.Parse(value);
        }

        public static string BuildGetIsInRaidModeCacheKey()
        {
            return $"{nameof(RaidModeService)}/{nameof(GetIsInRaidMode)}";
        }

        private async Task<bool> GetIsInRaidMode()
        {
            return await _appCache.GetOrAddAsync(BuildGetIsInRaidModeCacheKey(),
                GetIsInRaidModeInternal, 
                DateTimeOffset.Now.AddDays(30));
        }

        private async Task<bool> GetIsInRaidModeInternal()
        {
            var value = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.RaidModeEnabled)
                .Select(x => x.Value)
                .SingleAsync();

            return bool.Parse(value);
        }

        public static string BuildGetIsAutoRaidModeEnabledCacheKey()
        {
            return $"{nameof(RaidModeService)}/{nameof(GetIsAutoRaidModeEnabled)}";
        }

        private async Task<bool> GetIsAutoRaidModeEnabled()
        {
            return await _appCache.GetOrAddAsync(BuildGetIsAutoRaidModeEnabledCacheKey(),
                GetIsAutoRaidModeEnabledInternal, 
                DateTimeOffset.Now.AddDays(30));
        }

        private async Task<bool> GetIsAutoRaidModeEnabledInternal()
        {
            var value = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.AutoRaidModeEnabled)
                .Select(x => x.Value)
                .SingleAsync();

            return bool.Parse(value);
        }
    }

    public sealed record RaidAlertRequest(bool IsRaidDetected, bool IsInExistingRaidMode, bool IsAutoRaidModeEnabled) : IRequest;
}

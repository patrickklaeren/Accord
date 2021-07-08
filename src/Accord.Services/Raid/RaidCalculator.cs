using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Services.Helpers;

namespace Accord.Services.Raid
{
    public class RaidCalculator
    {
        private DateTime? _lastJoin;
        private int _joinsInLastRecordedCooldown;

        private readonly List<AccountCreationRange> _accountCreationDateRanges = new();

        private static readonly TimeSpan AccountCreationRange = TimeSpan.FromHours(2);
        private static readonly TimeSpan JoinCooldown = TimeSpan.FromSeconds(90);

        public bool CalculateIsRaid(UserJoin userJoin, int sequentialLimit, int accountCreationSimilarityLimit)
        {
            if (_lastJoin is null
                || (userJoin.JoinedDateTime - _lastJoin) > JoinCooldown)
            {
                _joinsInLastRecordedCooldown = 1;
            }
            else
            {
                _joinsInLastRecordedCooldown++;
            }

            var isAccountCreationRaid = IsAccountCreationRisk(userJoin, accountCreationSimilarityLimit);

            _lastJoin = userJoin.JoinedDateTime;

            return _joinsInLastRecordedCooldown >= sequentialLimit
                   || isAccountCreationRaid;
        }

        private bool IsAccountCreationRisk(UserJoin userJoin, int accountCreationSimilarityLimit)
        {
            var accountCreated = DiscordSnowflakeHelper.ToDateTimeOffset(userJoin.DiscordUserId);

            foreach (var existingRange in _accountCreationDateRanges.Where(x => x.IsExpired()).ToList())
            {
                _accountCreationDateRanges.Remove(existingRange);
            }

            if (_accountCreationDateRanges.Any(x => x.IsRisk(accountCreated.DateTime, accountCreationSimilarityLimit)))
            {
                return true;
            }

            var range = new AccountCreationRange(accountCreated.DateTime.Add(-AccountCreationRange),
                accountCreated.DateTime.Add(AccountCreationRange));

            _accountCreationDateRanges.Add(range);

            return false;
        }
    }

    public sealed record UserJoin(ulong DiscordUserId, DateTime JoinedDateTime);
}
using System;

namespace Accord.Services.Raid
{
    public class AccountCreationRange
    {
        private readonly DateTime _lower;
        private readonly DateTime _upper;
        private readonly DateTime _added = DateTime.Now;

        private static readonly TimeSpan AccountCreationThresholdRangeExpiry = TimeSpan.FromHours(2);

        private int _activations = 1;

        public AccountCreationRange(DateTime lower, DateTime upper)
        {
            _lower = lower;
            _upper = upper;
        }

        public bool IsRisk(DateTime candidate, int activationLimit)
        {
            var isInRange = candidate >= _lower && candidate <= _upper;

            if (isInRange)
            {
                _activations++;
            }

            return isInRange && _activations >= activationLimit;
        }

        public bool IsExpired() => _added.Add(AccountCreationThresholdRangeExpiry) < DateTime.Now;
    }
}
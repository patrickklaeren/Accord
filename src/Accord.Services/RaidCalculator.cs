using System;

namespace Accord.Services
{
    public class RaidCalculator
    {
        private DateTime? _lastJoin;
        private int _joinsInLastRecordedMinute;
        private static readonly TimeSpan OneMinute = TimeSpan.FromSeconds(60);

        public bool CalculateIsRaid(DateTimeOffset joined, int limitPerOneMinute)
        {
            if (_lastJoin is null)
            {
                _lastJoin = joined.DateTime;
                _joinsInLastRecordedMinute = 1;
                return false;
            }

            if ((joined.DateTime - _lastJoin) > OneMinute)
            {
                _lastJoin = joined.DateTime;
                _joinsInLastRecordedMinute = 1;
                return false;
            }

            _joinsInLastRecordedMinute++;
            _lastJoin = joined.DateTime;

            return _joinsInLastRecordedMinute >= limitPerOneMinute;
        }
    }
}
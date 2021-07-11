using System;
using System.Threading;

namespace Accord.Bot.Infrastructure
{
    public class BotState
    {
        private long _isCacheReady;
        public bool IsCacheReady
        {
            get => Interlocked.Read(ref _isCacheReady) == 1;
            set => Interlocked.Exchange(ref _isCacheReady, Convert.ToInt64(value));
        }

        public bool IsReady => IsCacheReady;
    }
}
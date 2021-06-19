using Remora.Discord.Core;

namespace Accord.Bot.Helpers
{
    public static class OptionalExtensions
    {
        public static string ToYesNo(this Optional<bool> input)
        {
            return input.HasValue ? "Yes" : "No";
        }
    }
}
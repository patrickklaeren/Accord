using Remora.Rest.Core;

namespace Accord.Bot.Extensions;

public static class OptionalExtensions
{
    public static string ToYesNo(this Optional<bool> input)
    {
        return input.HasValue ? "Yes" : "No";
    }
}
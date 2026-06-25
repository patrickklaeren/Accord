using System.Collections.Generic;

namespace Accord.Services.Starboard;

public static class StarboardConstants
{
    public const string EMOJI = "⭐";

    public static readonly Dictionary<int, string> WeightedStars = new()
    {
        [1] = "⭐",
        [6] = "🌟",
        [15] = "✨",
        [20] = "💫",
    };
}
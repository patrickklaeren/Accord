using System.Collections.Generic;

namespace Accord.Services.Starboard;

public static class StarboardConstants
{
    public static readonly string[] Emojis = ["⭐", "🌟", "✨", "💫"];

    public static readonly Dictionary<int, string> WeightedStars = new()
    {
        [1] = "⭐",
        [10] = "🌟",
        [15] = "✨",
        [20] = "💫",
    };
}
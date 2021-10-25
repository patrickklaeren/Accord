using System;

namespace Accord.Bot.Extensions;

public static class DateTimeExtensions
{
    public static string ToDiscordDateMarkdown(this DateTimeOffset dateTime) => $"<t:{dateTime.ToUnixTimeSeconds()}:R>";
}
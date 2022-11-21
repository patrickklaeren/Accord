using System;

namespace Accord.Services.Helpers;

public static class DiscordSnowflakeHelper
{
    public static DateTimeOffset ToDateTimeOffset(ulong discordSnowflake)
    {
        const long EPOCH = 1_420_070_400_000;
        var timeStamp = (long)(discordSnowflake / 4_194_304 + EPOCH);
        return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
    }
}
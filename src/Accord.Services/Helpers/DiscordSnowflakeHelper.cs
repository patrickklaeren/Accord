using System;

namespace Accord.Services.Helpers;

public static class DiscordSnowflakeHelper
{
    public static DateTimeOffset ToDateTimeOffset(ulong discordUserId)
    {
        const long EPOCH = 1_420_070_400_000;
        var timeStamp = (long)(discordUserId / 4_194_304 + EPOCH);
        return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
    }
}
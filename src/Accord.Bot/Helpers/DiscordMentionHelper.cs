using Remora.Discord.Core;

namespace Accord.Bot.Helpers
{
    public static class DiscordMentionHelper
    {
        public static string IdToMention(ulong discordUserId) => $"<@{discordUserId}>";
        public static string ToUserMention(this Snowflake snowflake) => $"<@{snowflake.Value}>";
    }
}

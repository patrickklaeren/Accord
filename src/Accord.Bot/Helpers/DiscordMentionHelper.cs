using Remora.Discord.Core;

namespace Accord.Bot.Helpers
{
    public static class DiscordMentionHelper
    {
        // https://discord.com/developers/docs/reference#message-formatting

        public static string UserIdToMention(ulong discordUserId) => $"<@{discordUserId}>";
        public static string ChannelIdToMention(ulong discordUserId) => $"<#{discordUserId}>";
        public static string ToUserMention(this Snowflake snowflake) => $"<@{snowflake.Value}>";
        public static string ToRoleMention(this Snowflake snowflake) => $"<@&{snowflake.Value}>";
    }
}

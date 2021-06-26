using Remora.Discord.Core;

namespace Accord.Bot.Helpers
{
    public static class DiscordMentionHelper
    {
        // https://discord.com/developers/docs/reference#message-formatting

        public static string UserIdToMention(ulong discordUserId) => $"<@{discordUserId}>";
        public static string ChannelIdToMention(ulong discordChannelId) => $"<#{discordChannelId}>";
        public static string RoleIdToMention(ulong discordRoleId) => $"<@&{discordRoleId}>";

        public static string ToUserMention(this Snowflake snowflake) => UserIdToMention(snowflake.Value);
        public static string ToRoleMention(this Snowflake snowflake) => RoleIdToMention(snowflake.Value);
        public static string ToChannelMention(this Snowflake snowflake) => ChannelIdToMention(snowflake.Value);
    }
}

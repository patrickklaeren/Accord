using Remora.Discord.Core;

namespace Accord.Bot.Helpers
{
    public static class DiscordFormatter
    {
        // https://discord.com/developers/docs/reference#message-formatting

        public static string UserIdToMention(ulong discordUserId) => $"<@{discordUserId}>";
        public static string ChannelIdToMention(ulong discordChannelId) => $"<#{discordChannelId}>";
        public static string RoleIdToMention(ulong discordRoleId) => $"<@&{discordRoleId}>";

        public static string ToUserMention(this Snowflake snowflake) => UserIdToMention(snowflake.Value);
        public static string ToRoleMention(this Snowflake snowflake) => RoleIdToMention(snowflake.Value);
        public static string ToChannelMention(this Snowflake snowflake) => ChannelIdToMention(snowflake.Value);

        public static string GetJumpUrl(ulong guildId, ulong channelId, ulong messageId)
            => $"https://discord.com/channels/{guildId}/{channelId}/{messageId}";

        public static string ToFormattedUrl(string text, string url) => $"[{text}]({url})";
    }
}

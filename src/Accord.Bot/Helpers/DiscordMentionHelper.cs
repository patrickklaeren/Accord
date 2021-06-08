namespace Accord.Bot.Helpers
{
    public static class DiscordMentionHelper
    {
        public static string IdToMention(ulong discordUserId) => $"<@{discordUserId}>";
    }
}

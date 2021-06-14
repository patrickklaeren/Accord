namespace Accord.Bot.Infrastructure
{
    public class DiscordConfiguration
    {
        public ulong GuildId { get; set; }
        public string CdnBaseUrl { get; set; } = null!;
    }
}
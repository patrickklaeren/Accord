namespace Accord.Bot.Infrastructure;

public class DiscordConfiguration
{
    public ulong GuildId { get; set; }
    public ulong HelpForumChannelId { get; set; }
    public string CdnBaseUrl { get; set; } = null!;
}
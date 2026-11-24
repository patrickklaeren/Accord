namespace Accord.Services;

public class DiscordConfiguration
{
    public ulong GuildId { get; set; }
    public string TicketToolPrefix { get; set; } = "$";
    public string CdnBaseUrl { get; set; } = null!;
}

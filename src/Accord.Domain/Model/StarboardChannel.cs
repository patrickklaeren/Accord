namespace Accord.Domain.Model;

public class StarboardChannel
{
    public int Id { get; set; }
    public ulong? DiscordChannelBeingStarredInId { get; set; }
    public ulong DiscordStarboardChannelId { get; set; }
}
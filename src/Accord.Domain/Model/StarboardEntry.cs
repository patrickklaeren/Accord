using System.Collections.Generic;

namespace Accord.Domain.Model;

public class StarboardEntry
{
    public int Id { get; set; }
    
    public ulong StarredDiscordMessageId { get; set; }
    public ulong StarredDiscordMessageChannelId { get; set; }
    public ulong StarredDiscordUserId { get; set; }
    public User? StarredDiscordUser { get; set; }
    
    public int Stars { get; set; }

    public ICollection<StarboardEntryOutput> Outputs { get; set; } = new HashSet<StarboardEntryOutput>();
}
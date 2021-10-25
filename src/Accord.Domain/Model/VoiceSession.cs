using System;

namespace Accord.Domain.Model;

public class VoiceSession
{
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string DiscordSessionId { get; set; } = null!;

    public ulong DiscordChannelId { get; set; }

    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset? EndDateTime { get; set; }
    public double? MinutesInVoiceChannel { get; set; }

    public bool HasBeenCountedToXp { get; set; }
}
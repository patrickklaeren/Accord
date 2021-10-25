using System;

namespace Accord.Domain.Model;

public class NamePattern
{
    public int Id { get; set; }
    public string Pattern { get; set; } = null!;
    public PatternType Type { get; set; }
    public OnNamePatternDiscovery OnDiscovery { get; set; }

    public ulong AddedByUserId { get; set; }
    public virtual User AddedByUser { get; set; } = null!;
    public DateTimeOffset AddedDateTime { get; set; }
}

public enum PatternType
{
    Allowed,
    Blocked
}

public enum OnNamePatternDiscovery
{
    DoNothing = 0,
    Alert = 1,
    Kick = 2,
    Ban = 3
}
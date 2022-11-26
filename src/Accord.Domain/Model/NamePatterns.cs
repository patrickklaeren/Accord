using System;

namespace Accord.Domain.Model;

public class NamePattern
{
    public int Id { get; set; }
    public required string Pattern { get; set; }
    public PatternType Type { get; set; }
    public OnNamePatternDiscovery OnDiscovery { get; set; }

    public ulong AddedByUserId { get; set; }
    public User? AddedByUser { get; set; }
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
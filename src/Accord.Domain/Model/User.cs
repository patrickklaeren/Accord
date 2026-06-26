using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model;

public class User
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public DateTimeOffset? JoinedGuildDateTime { get; set; }
    public DateTimeOffset? LeftGuildDateTime { get; set; }
    
    public string? Username { get; set; }
    public string? Nickname { get; set; }

    public DateTimeOffset FirstSeenDateTime { get; set; }
    public DateTimeOffset LastSeenDateTime { get; set; }

    public int ParticipationRank { get; set; }
    public int ParticipationPoints { get; set; }
    public double ParticipationPercentile { get; set; }

    public DateTimeOffset? TimedOutUntil { get; set; }
    public DateTimeOffset? VoiceAutoUnmuteAtDateTime { get; set; }

    public ICollection<UserMessage> Messages { get; set; } = new HashSet<UserMessage>();
    public ICollection<UserHistory> Histories { get; set; } = new HashSet<UserHistory>();
    public ICollection<UserHistory> HistoriesAddedByUser { get; set; } = new HashSet<UserHistory>();
}
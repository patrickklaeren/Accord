using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model;

public class UserHistory
{
    public int Id { get; set; }
    
    public UserHistoryType Type { get; set; }
    public required string Content { get; set; }
    
    public ulong UserId { get; set; }
    [InverseProperty(nameof(User.Histories))]
    public User? User { get; set; }
    
    public ulong AddedByUserId { get; set; }
    [InverseProperty(nameof(User.HistoriesAddedByUser))]
    public User? AddedByUser { get; set; }
    public DateTimeOffset AddedDateTime { get; set; }
}

public enum UserHistoryType
{
    Note = 0,
    Warning = 1,
    Mute = 2,
    Unmute = 3,
    Kick = 4,
    Ban = 5,
    Unban = 6,
}
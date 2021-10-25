using System;
using System.ComponentModel.DataAnnotations;

namespace Accord.Domain.Model;

public class UserReminder
{
    [Key]
    public int Id { get; set; }

    public ulong UserId { get; set; }
    public virtual User User { get; set; } = null!;
        
    public ulong DiscordChannelId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset RemindAt { get; set; }
    public string Message { get; set; } = null!;
}
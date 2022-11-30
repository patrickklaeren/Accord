using System;
using System.ComponentModel.DataAnnotations;

namespace Accord.Domain.Model;

public class UserReminder
{
    [Key]
    public int Id { get; set; }

    public ulong UserId { get; set; }
    public User? User { get; set; }
    
    public ulong DiscordChannelId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset RemindAt { get; set; }
    public required string Message { get; set; }
}
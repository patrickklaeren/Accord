using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model;

public class UserMessage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public ulong UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public ulong DiscordChannelId { get; set; }
    public DateTimeOffset SentDateTime { get; set; }
}
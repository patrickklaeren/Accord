using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model;

public class UserMessage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public ulong UserId { get; set; }
    public User? User { get; set; }

    public ulong DiscordChannelId { get; set; }
    public DateTimeOffset SentDateTime { get; set; }
}
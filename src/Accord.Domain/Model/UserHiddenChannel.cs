using System;
using System.ComponentModel.DataAnnotations;

namespace Accord.Domain.Model
{
    public class UserHiddenChannel
    {
        [Key]
        public int Id { get; set; }
        
        public ulong UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        public ulong? ParentDiscordChannelId { get; set; }
        public ulong DiscordChannelId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
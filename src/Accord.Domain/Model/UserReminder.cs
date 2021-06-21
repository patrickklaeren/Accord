using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model
{
    public class UserReminder
    {
        [Key]
        public int Id { get; set; }

        public ulong UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        public ulong DiscordChannelId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime RemindAt { get; set; }

        [Column(TypeName = "text")]
        public string Message { get; set; } = null!;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public DateTimeOffset? JoinedGuildDateTime { get; set; }
        public string? UsernameWithDiscriminator { get; set; }
        public string? Nickname { get; set; }

        public DateTimeOffset FirstSeenDateTime { get; set; }
        public DateTimeOffset LastSeenDateTime { get; set; }

        public float Xp { get; set; }

        public ICollection<UserMessage> Messages { get; set; } = new HashSet<UserMessage>();
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public DateTimeOffset FirstSeenDateTime { get; set; }
        public DateTimeOffset LastSeenDateTime { get; set; }

        public float Xp { get; set; }
    }
}
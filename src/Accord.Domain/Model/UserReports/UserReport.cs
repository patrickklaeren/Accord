using System;
using System.Collections.Generic;

namespace Accord.Domain.Model.UserReports
{
    public class UserReport
    {
        public int Id { get; set; }

        public ulong OutboxDiscordChannelId { get; set; }
        public ulong InboxDiscordChannelId { get; set; }

        public ulong OpenedByUserId { get; set; }
        public virtual User OpenedByUser { get; set; } = null!;
        public DateTimeOffset OpenedDateTime { get; set; }

        public ulong? ClosedByUserId { get; set; }
        public virtual User ClosedByUser { get; set; } = null!;
        public DateTimeOffset? ClosedDateTime { get; set; }

        public ICollection<UserReportMessage> Messages { get; set; } = new HashSet<UserReportMessage>();
    }
}

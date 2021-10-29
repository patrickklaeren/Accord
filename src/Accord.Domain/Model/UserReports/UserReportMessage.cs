using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model.UserReports;

public class UserReportMessage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public ulong DiscordProxyMessageId { get; set; }
    public int UserReportId { get; set; }
    public virtual UserReport UserReport { get; set; } = null!;

    public ulong AuthorUserId { get; set; }
    public virtual User AuthorUser { get; set; } = null!;
    public DateTimeOffset SentDateTime { get; set; }

    public string Content { get; set; } = null!;

    public bool IsInternal { get; set; }
}
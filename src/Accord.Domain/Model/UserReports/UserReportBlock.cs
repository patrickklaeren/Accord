using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model.UserReports;

public class UserReportBlock
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public ulong BlockedByUserId { get; set; }
    public virtual User BlockedByUser { get; set; } = null!;
    public DateTimeOffset BlockedDateTime { get; set; }
}
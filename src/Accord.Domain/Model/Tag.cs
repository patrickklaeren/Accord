using System;
using System.Collections.Generic;

namespace Accord.Domain.Model;

public class Tag
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public int Uses { get; set; }
    
    public ulong AddedByUserId { get; set; }
    public User? AddedByUser { get; set; }
    public DateTimeOffset AddedDateTime { get; set; }
    
    public ICollection<TagAlias> Aliases { get; set; } = new HashSet<TagAlias>();
}
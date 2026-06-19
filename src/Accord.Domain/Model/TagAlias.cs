using System;

namespace Accord.Domain.Model;

public class TagAlias
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
    
    public ulong AddedByUserId { get; set; }
    public User? AddedByUser { get; set; }
    public DateTimeOffset AddedDateTime { get; set; }
}
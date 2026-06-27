using System;

namespace Accord.Domain.Model;

public class RssFeedPost
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public DateTimeOffset PublishedAtDateTime { get; set; }
    
    public int RssFeedId { get; set; }
    public RssFeed? RssFeed { get; set; }
}
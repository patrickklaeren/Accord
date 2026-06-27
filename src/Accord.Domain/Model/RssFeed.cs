using System;
using System.Collections.Generic;

namespace Accord.Domain.Model;

public class RssFeed
{
    public int Id { get; set; }
    public required string Url { get; set; }
    public ulong DiscordChannelId { get; set; }
    
    public string? LastFailedFetchResponse { get; set; }
    public int NumberOfFailedFetches { get; set; }
    
    public DateTimeOffset AddedDateTime { get; set; }
    public DateTimeOffset? LastFetchDateTime { get; set; }
    public DateTimeOffset? NextFetchDateTime { get; set; }

    public ICollection<RssFeedPost> Posts { get; set; } = new HashSet<RssFeedPost>();
}
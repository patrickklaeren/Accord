using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Accord.Services.Rss;

public sealed record RssFeedItemDto(string Title, string Url, DateTimeOffset PublishedAt);

[RegisterScoped]
public class RssFeedReaderService(HttpClient httpClient)
{
    public async Task<ServiceResponse<IReadOnlyCollection<RssFeedItemDto>>> GetFeed(string feedUrl)
    {
        using var response = await httpClient.GetAsync(feedUrl);

        if (!response.IsSuccessStatusCode)
            return ServiceResponse.Fail<IReadOnlyCollection<RssFeedItemDto>>($"Request failed with status code {response.StatusCode}");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(reader);

        if (feed is null)
            return ServiceResponse.Fail<IReadOnlyCollection<RssFeedItemDto>>("Failed deserialising feed");

        var items = feed
            .Items
            .Where(x => !string.IsNullOrWhiteSpace(x.Title?.Text))
            .Where(x => x.Links.Any())
            .Select(item => new RssFeedItemDto(
                item.Title!.Text.Trim(),
                item.Links.First().Uri.ToString(),
                item.PublishDate
            ))
            .ToList();

        return ServiceResponse.Ok<IReadOnlyCollection<RssFeedItemDto>>(items);
    }
}

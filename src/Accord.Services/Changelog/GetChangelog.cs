using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Changelog;

public sealed record GetChangelogRequest : IRequest<ServiceResponse<ChangelogDto>>;

public sealed record ChangelogDto(string Name, DateTimeOffset PublishedAt, string Body, string Url);

internal class GetChangelogHandler(HttpClient httpClient) : IRequestHandler<GetChangelogRequest, ServiceResponse<ChangelogDto>>
{
    public async Task<ServiceResponse<ChangelogDto>> Handle(GetChangelogRequest request, CancellationToken cancellationToken)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/patrickklaeren/Accord/releases/latest");
        httpRequest.Headers.UserAgent.ParseAdd("Accord-Bot/1.0");

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse.Fail<ChangelogDto>($"Failed getting latest release, with code {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var release = JsonSerializer.Deserialize<GitHubReleaseResponse>(content);

        if (release is null)
        {
            return ServiceResponse.Fail<ChangelogDto>("Failed to parse release data");
        }

        var dto = new ChangelogDto(release.Name ?? release.TagName, release.PublishedAt, release.Body ?? string.Empty, release.HtmlUrl);

        return ServiceResponse.Ok(dto);
    }

    private sealed record GitHubReleaseResponse(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("published_at")] DateTimeOffset PublishedAt,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("html_url")] string HtmlUrl
    );
}

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Permissions;

namespace Accord.Services.Shlink;

[RegisterScoped]
public class ShlinkApiService(HttpClient httpClient, UserPermissionService userPermissionService)
{
    public async Task<ServiceResponse<string>> CreateLink(PermissionUser user, string url)
    {
        if (!await userPermissionService.HasPermission(user, PermissionType.CreateShortUrls))
        {
            return ServiceResponse.Fail<string>("Missing permission to shorten URL");
        }

        var request = new ShlinkShortenUrlRequest
        {
            Url = url,
        };

        var payload = JsonSerializer.Serialize(request);
        
        var response = await httpClient.PostAsync("rest/v3/short-urls",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse.Fail<string>($"Failed to create shorten URL with response {response.StatusCode} ({response.ReasonPhrase})");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var deserialised = JsonSerializer.Deserialize<ShlinkShortenUrlResponse>(responseContent);
        return ServiceResponse.Ok(deserialised!.Url);
    }
}

internal class ShlinkShortenUrlRequest
{
    [JsonPropertyName("longUrl")]
    public required string Url { get; set; }
}

internal class ShlinkShortenUrlResponse
{
    [JsonPropertyName("shortUrl")]
    public required string Url { get; set; }
}
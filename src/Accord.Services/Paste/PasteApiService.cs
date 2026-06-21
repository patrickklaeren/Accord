using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Accord.Services.Paste;

[RegisterScoped]
public class PasteApiService(HttpClient client)
{
    public async Task<ServiceResponse<string>> CreatePaste(string text,
        string? extension = null,
        string? title = null,
        int? expires = null,
        bool? burnAfterReading = null,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new PasteCreateRequestBody
        {
            Text = text,
            Extension = extension,
            Title = title,
            Expires = expires,
            BurnAfterReading = burnAfterReading,
            Password = password
        };

        var json = JsonSerializer.Serialize(payload);
        var response = await client.PostAsync("/",
            new StringContent(json, Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse.Fail<string>($"Paste creation failed with status code {response.StatusCode} ({response.ReasonPhrase})");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<PasteCreateResponseBody>(responseContent);

        if (result?.Path is null)
        {
            return ServiceResponse.Fail<string>("Paste creation response did not deserialise into known type");
        }

        var baseUrl = client.BaseAddress?.ToString().TrimEnd('/');
        var url = $"{baseUrl}{result.Path}";

        return ServiceResponse.Ok(url);
    }
}

internal class PasteCreateRequestBody
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("expires")]
    public int? Expires { get; set; }

    [JsonPropertyName("burn_after_reading")]
    public bool? BurnAfterReading { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

internal class PasteCreateResponseBody
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Accord.Services.Ollama;

[RegisterScoped]
public sealed class OllamaService(IHttpClientFactory httpClientFactory, IOptions<OllamaOptions> options)
{
    public bool IsConfigured => GetGenerateEndpoint() is not null;

    public async Task<string?> GenerateJson(string prompt, CancellationToken cancellationToken)
    {
        var endpoint = GetGenerateEndpoint();
        if (endpoint is null)
        {
            return null;
        }

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var request = new OllamaGenerateRequest(options.Value.Model, prompt);
        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);
        if (result is not { Done: true } || string.IsNullOrWhiteSpace(result.Response))
        {
            throw new JsonException("Ollama did not return a completed response.");
        }

        return result.Response;
    }

    private Uri? GetGenerateEndpoint()
    {
        if (string.IsNullOrWhiteSpace(options.Value.BaseUrl)
            || string.IsNullOrWhiteSpace(options.Value.Model)
            || !Uri.TryCreate(options.Value.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https"))
        {
            return null;
        }

        return new Uri(baseUri, "api/generate");
    }
}

internal sealed record OllamaGenerateRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt)
{
    [JsonPropertyName("stream")]
    public bool Stream => false;

    [JsonPropertyName("think")]
    public bool Think => false;

    [JsonPropertyName("format")]
    public string Format => "json";
}

internal sealed record OllamaGenerateResponse(
    [property: JsonPropertyName("response")] string? Response,
    [property: JsonPropertyName("done")] bool Done);

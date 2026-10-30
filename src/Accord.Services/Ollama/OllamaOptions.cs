namespace Accord.Services.Ollama;

public sealed class OllamaOptions
{
    public const string SECTION_NAME = "Ollama";

    public string? BaseUrl { get; set; }
    public string Model { get; set; } = "qwen3:0.6b";
}

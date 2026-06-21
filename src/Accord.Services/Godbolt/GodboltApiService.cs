using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Accord.Services.Godbolt;

[RegisterScoped]
public class GodboltApiService(HttpClient httpClient)
{
    private const string GODBOLT_API_SCHEME = "https://godbolt.org/api/compiler/dotnettrunk{0}coreclr/compile";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    public async Task<ServiceResponse<string>> Compile(string code, string language, string arguments)
    {
        var request = new CompileRequest
        {
            Source = code,
            Compiler = $"dotnettrunk{language}coreclr",
            Options = new CompilerOptions
            {
                UserArguments = string.IsNullOrEmpty(arguments) 
                    ? string.Empty 
                    : arguments,
                Filters = new AssemblyFilters
                {
                    CommentOnly = true,
                    Directives = true,
                    Labels = true,
                    Trim = true,
                    Demangle = true,
                    Intel = true,
                }
            }
        };

        var url = string.Format(GODBOLT_API_SCHEME, language);
        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var response = await httpClient.PostAsync(url, new StringContent(requestJson, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse.Fail<string>($"Godbolt request failed with response code {response.StatusCode} ({response.ReasonPhrase})");
        }

        var result = await response.Content.ReadAsStringAsync();
        return ServiceResponse.Ok(result);
    }
}

internal class CompileRequest
{
    [JsonPropertyName("source")]
    public required string Source { get; set; }
    
    [JsonPropertyName("compiler")]
    public required string Compiler { get; set; }
    
    [JsonPropertyName("options")]
    public required CompilerOptions Options { get; set; }
}    

internal class CompilerOptions
{
    [JsonPropertyName("userArguments")]
    public required string UserArguments { get; set; } = "";
    
    [JsonPropertyName("filters")]
    public required AssemblyFilters Filters { get; set; }
}

internal class AssemblyFilters
{
    [JsonPropertyName("commentOnly")]
    public bool CommentOnly { get; set; }
    
    [JsonPropertyName("directives")]
    public bool Directives { get; set; }
    
    [JsonPropertyName("labels")]
    public bool Labels { get; set; }
    
    [JsonPropertyName("trim")]
    public bool Trim { get; set; }
    
    [JsonPropertyName("demangle")]
    public bool Demangle { get; set; }
    
    [JsonPropertyName("intel")]
    public bool Intel { get; set; }
}
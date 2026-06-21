using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Accord.Services.CodeEvaluation;

[RegisterScoped]
public class CSharpReplApiService(HttpClient client)
{
    public async Task<ServiceResponse<EvalResultDto>> Execute(string code, CancellationToken cancellationToken)
    {
        var response = await client.PostAsync("Eval",
            new StringContent(code, Encoding.UTF8, "text/plain"),
            cancellationToken);
        
        if (!response.IsSuccessStatusCode 
            && response.StatusCode != HttpStatusCode.BadRequest)
        {
            return ServiceResponse.Fail<EvalResultDto>($"Request failed with status code {response.StatusCode} ({response.ReasonPhrase})");
        }
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var replResult = JsonSerializer.Deserialize<CSharpReplApiResult>(responseContent);

        if (replResult is null)
        {
            return ServiceResponse.Fail<EvalResultDto>("Response did not deserialise into known type");
        }

        var result = new EvalResultDto(replResult.Code,
            replResult.ReturnTypeName,
            replResult.ReturnValue,
            replResult.Exception,
            replResult.ExceptionType,
            replResult.ConsoleOut,
            replResult.ExecutionTime,
            replResult.CompileTime);

        return ServiceResponse.Ok(result);
    }
}

public sealed record EvalResultDto(
    string Code,
    string? ReturnTypeName,
    object? ReturnValue,
    string? Exception,
    string? ExceptionType,
    string? ConsoleOut,
    TimeSpan ExecutionTime,
    TimeSpan CompileTime);
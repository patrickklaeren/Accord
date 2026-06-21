using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Paste;
using MediatR;

namespace Accord.Services.CodeEvaluation;

public sealed record ExecuteEvalRequest(string Content) : IRequest<ServiceResponse<ExecuteEvalResultDto>>;

public class ExecuteEvalHandler(CSharpReplApiService cSharpReplApiService,
    PasteApiService pasteApiService) 
    : IRequestHandler<ExecuteEvalRequest, ServiceResponse<ExecuteEvalResultDto>>
{
    public async Task<ServiceResponse<ExecuteEvalResultDto>> Handle(ExecuteEvalRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await cSharpReplApiService.Execute(request.Content, cancellationToken);
        
        ExecuteEvalResultDto? result = null;
        ServiceResponse<string>? resultPaste = null;
        
        if (response.Success)
        {
            var evalResult = response.Value!;

            if (evalResult.ReturnValue is not null)
            {
                resultPaste = await pasteApiService.CreatePaste(evalResult.ReturnValue.ToString()!,
                    "json",
                    cancellationToken: cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(evalResult.ConsoleOut))
            {
                resultPaste = await pasteApiService.CreatePaste(evalResult.ConsoleOut,
                    "bash",
                    cancellationToken: cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(evalResult.Exception))
            {
                var paste = $"{evalResult.ExceptionType}{Environment.NewLine}{evalResult.Exception}";
                
                resultPaste = await pasteApiService.CreatePaste(paste,
                    "cs",
                    cancellationToken: cancellationToken);
            }

            result = new ExecuteEvalResultDto(evalResult.Code, 
                evalResult.ReturnTypeName, 
                evalResult.ReturnValue, 
                evalResult.Exception, 
                evalResult.ExceptionType,
                evalResult.ConsoleOut, 
                evalResult.ExecutionTime, 
                evalResult.CompileTime, 
                resultPaste?.Value);
        }

        return response.Success 
            ? ServiceResponse.Ok(result!) 
            : ServiceResponse.Fail<ExecuteEvalResultDto>(response.ErrorMessage);
    }
}

public sealed record ExecuteEvalResultDto(
    string Code,
    string? ReturnTypeName,
    object? ReturnValue,
    string? Exception,
    string? ExceptionType,
    string? ConsoleOut,
    TimeSpan ExecutionTime,
    TimeSpan CompileTime,
    string? ResultPasteUrl);
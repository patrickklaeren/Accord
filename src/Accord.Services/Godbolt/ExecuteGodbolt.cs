using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Paste;
using MediatR;

namespace Accord.Services.Godbolt;

public sealed record ExecuteGodboltRequest(string Code, string Language, string Arguments)
    : IRequest<ServiceResponse<ExecuteGodboltResultDto>>;

public class ExecuteGodboltHandler(GodboltApiService godboltApiService,
    PasteApiService pasteApiService)
    : IRequestHandler<ExecuteGodboltRequest, ServiceResponse<ExecuteGodboltResultDto>>
{
    public async Task<ServiceResponse<ExecuteGodboltResultDto>> Handle(ExecuteGodboltRequest request,
        CancellationToken cancellationToken)
    {
        var response = await godboltApiService.Compile(request.Code, request.Language, request.Arguments);

        if (!response.Success)
            return ServiceResponse.Fail<ExecuteGodboltResultDto>(response.ErrorMessage);

        var result = response.Value!;

        var pasteResponse = await pasteApiService.CreatePaste(result,
            "txt",
            cancellationToken: cancellationToken);

        var dto = new ExecuteGodboltResultDto(result, pasteResponse.Value);

        return ServiceResponse.Ok(dto);
    }
}

public sealed record ExecuteGodboltResultDto(
    string Result,
    string? ResultPasteUrl);

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.CodeEvaluation;

public sealed record ExecuteEvalRequest(string Content) : IRequest<ServiceResponse<EvalResultDto>>;

public class ExecuteEvalHandler(CSharpReplApiService cSharpReplApiService) 
    : IRequestHandler<ExecuteEvalRequest, ServiceResponse<EvalResultDto>>
{
    public async Task<ServiceResponse<EvalResultDto>> Handle(ExecuteEvalRequest request, 
        CancellationToken cancellationToken)
    {
        return await cSharpReplApiService.Execute(request.Content, cancellationToken);
    }
}
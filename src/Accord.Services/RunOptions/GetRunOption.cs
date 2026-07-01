using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.RunOptions;

public sealed record GetRunOptionRequest(RunOptionKey Key) : IRequest<string>;

internal class GetRunOptionHandler(RunOptionService runOptionService) : IRequestHandler<GetRunOptionRequest, string>
{
    public async Task<string> Handle(GetRunOptionRequest request, CancellationToken cancellationToken)
    {
        return await runOptionService.GetOption<string>(request.Key);
    }
}
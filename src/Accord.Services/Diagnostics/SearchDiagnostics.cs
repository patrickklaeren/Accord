using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Diagnostics;

public sealed record SearchDiagnosticsRequest(string Query) : IRequest<IReadOnlyList<DiagnosticInfo>>;

public class SearchDiagnosticsHandler(DiagnosticsCatalog catalog)
    : IRequestHandler<SearchDiagnosticsRequest, IReadOnlyList<DiagnosticInfo>>
{
    public Task<IReadOnlyList<DiagnosticInfo>> Handle(SearchDiagnosticsRequest request, CancellationToken cancellationToken)
        => Task.FromResult(catalog.Search(request.Query));
}
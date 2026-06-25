using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Diagnostics;

public sealed record SuggestDiagnosticsRequest(string Query, int Limit) : IRequest<IReadOnlyList<DiagnosticInfo>>;

public class SuggestDiagnosticsHandler(DiagnosticsCatalog catalog)
    : IRequestHandler<SuggestDiagnosticsRequest, IReadOnlyList<DiagnosticInfo>>
{
    public Task<IReadOnlyList<DiagnosticInfo>> Handle(SuggestDiagnosticsRequest request, CancellationToken cancellationToken)
        => Task.FromResult(catalog.Suggest(request.Query, request.Limit));
}
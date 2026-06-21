using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.LinkPreviews;

public sealed record PreviewLabRazorFyiRequest(Uri url) : IRequest<string?>;

public class PreviewLabRazorFyi : IRequestHandler<PreviewLabRazorFyiRequest, string?>
{
    public Task<string?> Handle(PreviewLabRazorFyiRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(LabRazorFyiPreviewService.TryGetPreview(request.url.Fragment));
    }
}

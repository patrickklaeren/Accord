using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Paste;

public sealed record CreatePasteRequest(
    string Text,
    string? Extension = null,
    string? Title = null
) : IRequest<ServiceResponse<string>>;

public class CreatePasteHandler(PasteApiService pasteApiService)
    : IRequestHandler<CreatePasteRequest, ServiceResponse<string>>
{
    public async Task<ServiceResponse<string>> Handle(CreatePasteRequest request,
        CancellationToken cancellationToken)
    {
        return await pasteApiService.CreatePaste(
            request.Text,
            request.Extension,
            request.Title,
            null,
            null,
            null,
            cancellationToken);
    }
}

using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Tags;

public sealed record GetTagContentRequest(string Name) : IRequest<string?>;

public class GetTagContentHandler(TagService tagService) : IRequestHandler<GetTagContentRequest, string?>
{
    public async Task<string?> Handle(GetTagContentRequest request, CancellationToken cancellationToken)
    {
        return await tagService.GetTagContent(request.Name);
    }
}

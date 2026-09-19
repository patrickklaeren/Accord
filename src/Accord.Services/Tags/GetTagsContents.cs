using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Tags;

public sealed record GetTagsContentsRequest(string[] Names) : IRequest<string[]>;

public class GetTagsContentsHandler(TagService tagService) : IRequestHandler<GetTagsContentsRequest, string[]>
{
    public async Task<string[]> Handle(GetTagsContentsRequest request, CancellationToken cancellationToken)
    {
        return await tagService.GetTagsContents(request.Names);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Tags;

public sealed record GetTagsContentsRequest(IReadOnlyCollection<string> Names) : IRequest<IReadOnlyCollection<string>>;

public class GetTagsContentsHandler(TagService tagService) : IRequestHandler<GetTagsContentsRequest, IReadOnlyCollection<string>>
{
    public async Task<IReadOnlyCollection<string>> Handle(GetTagsContentsRequest request, CancellationToken cancellationToken)
    {
        return await tagService.GetTagsContents(request.Names);
    }
}

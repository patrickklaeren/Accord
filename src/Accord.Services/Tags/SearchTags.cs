using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Tags;

public sealed record SearchTagsRequest(string SearchTerm) : IRequest<List<TagSearchResult>>;

public class SearchTagsHandler(TagService tagService) : IRequestHandler<SearchTagsRequest, List<TagSearchResult>>
{
    public async Task<List<TagSearchResult>> Handle(SearchTagsRequest request, CancellationToken cancellationToken)
    {
        return await tagService.SearchTags(request.SearchTerm);
    }
}

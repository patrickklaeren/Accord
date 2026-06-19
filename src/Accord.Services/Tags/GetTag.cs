using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Tags;

public sealed record GetTagRequest(string Name) : IRequest<TagDto?>;

public class GetTagHandler(TagService tagService) : IRequestHandler<GetTagRequest, TagDto?>
{
    public async Task<TagDto?> Handle(GetTagRequest request, CancellationToken cancellationToken)
    {
        return await tagService.GetTag(request.Name);
    }
}

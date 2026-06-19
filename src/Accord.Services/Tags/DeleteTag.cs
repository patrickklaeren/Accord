using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Tags;

public sealed record DeleteTagRequest(string Name, PermissionUser User) : IRequest<ServiceResponse>;

public class DeleteTagHandler(TagService tagService) : IRequestHandler<DeleteTagRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(DeleteTagRequest request, CancellationToken cancellationToken)
    {
        return await tagService.DeleteTag(request.Name, request.User);
    }
}

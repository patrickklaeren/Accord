using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Tags;

public sealed record UpdateTagRequest(int TagId, string Content, PermissionUser User) : IRequest<ServiceResponse>;

public class UpdateTagHandler(TagService tagService) : IRequestHandler<UpdateTagRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(UpdateTagRequest request, CancellationToken cancellationToken)
    {
        return await tagService.UpdateTag(request.TagId, request.Content, request.User);
    }
}

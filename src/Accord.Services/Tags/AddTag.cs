using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Tags;

public sealed record AddTagRequest(string Name, string Content, PermissionUser User) : IRequest<ServiceResponse>;

public class AddTagHandler(TagService tagService) : IRequestHandler<AddTagRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(AddTagRequest request, CancellationToken cancellationToken)
    {
        return await tagService.AddTag(request.Name, request.Content, request.User);
    }
}

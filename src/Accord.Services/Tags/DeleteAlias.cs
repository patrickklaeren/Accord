using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Tags;

public sealed record DeleteAliasRequest(string Name, PermissionUser User) : IRequest<ServiceResponse>;

public class DeleteAliasHandler(TagService tagService) : IRequestHandler<DeleteAliasRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(DeleteAliasRequest request, CancellationToken cancellationToken)
    {
        return await tagService.DeleteAlias(request.Name, request.User);
    }
}

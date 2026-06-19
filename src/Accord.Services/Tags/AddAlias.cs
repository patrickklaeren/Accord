using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Tags;

public sealed record AddAliasRequest(string Name, string NewAlias, PermissionUser User) : IRequest<ServiceResponse>;

public class AddAliasHandler(TagService tagService) : IRequestHandler<AddAliasRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(AddAliasRequest request, CancellationToken cancellationToken)
    {
        return await tagService.AddAlias(request.Name, request.NewAlias, request.User);
    }
}

using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.Permissions;

public sealed record UserHasPermissionRequest(PermissionUser User, PermissionType Permission) : IRequest<bool>;

internal class UserHasPermission(UserPermissionService userPermissionService) 
    : IRequestHandler<UserHasPermissionRequest, bool>
{
    public async Task<bool> Handle(UserHasPermissionRequest request, CancellationToken cancellationToken)
    {
        return await userPermissionService.HasPermission(request.User, request.Permission);
    }
}
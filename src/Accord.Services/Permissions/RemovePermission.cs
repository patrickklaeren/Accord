using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.Permissions;

public sealed record RemovePermissionForRoleRequest(ulong DiscordRoleId, PermissionType Permission) : IRequest;
public sealed record RemovePermissionForUserRequest(ulong DiscordUserId, PermissionType Permission) : IRequest;

public class RemovePermissionHandler(UserPermissionService userPermissionService)
    : IRequestHandler<RemovePermissionForRoleRequest>, IRequestHandler<RemovePermissionForUserRequest>
{
    public async Task Handle(RemovePermissionForRoleRequest request, CancellationToken ct)
    {
        await userPermissionService.RemovePermissionFromRole(request.DiscordRoleId, request.Permission, ct);
    }

    public async Task Handle(RemovePermissionForUserRequest request, CancellationToken ct)
    {
        await userPermissionService.RemovePermissionFromUser(request.DiscordUserId, request.Permission, ct);
    }
}

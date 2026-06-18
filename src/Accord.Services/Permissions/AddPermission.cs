using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

public sealed record AddPermissionForRoleRequest(ulong DiscordRoleId, PermissionType Permission) : IRequest;
public sealed record AddPermissionForUserRequest(ulong DiscordUserId, PermissionType Permission) : IRequest;

public class AddPermissionHandler(UserPermissionService userPermissionService) : IRequestHandler<AddPermissionForUserRequest>, IRequestHandler<AddPermissionForRoleRequest>
{
    public async Task Handle(AddPermissionForUserRequest request, CancellationToken cancellationToken)
    {
        await userPermissionService.AddPermissionToUser(request.DiscordUserId, request.Permission, cancellationToken);
    }

    public async Task Handle(AddPermissionForRoleRequest request, CancellationToken cancellationToken)
    {
        await userPermissionService.AddPermissionToRole(request.DiscordRoleId, request.Permission, cancellationToken);
    }
}
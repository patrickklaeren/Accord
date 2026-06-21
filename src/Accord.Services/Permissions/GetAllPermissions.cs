using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

public sealed record GetAllPermissionsRequest : IRequest<GetAllPermissionsResponse>;

public sealed record GetAllPermissionsResponse(
    IReadOnlyCollection<RolePermissionDto> RolePermissions,
    IReadOnlyCollection<UserPermissionDto> UserPermissions);

public sealed record RolePermissionDto(ulong RoleId, PermissionType Type);
public sealed record UserPermissionDto(ulong UserId, PermissionType Type);

public class GetAllPermissionsHandler(AccordContext db)
    : IRequestHandler<GetAllPermissionsRequest, GetAllPermissionsResponse>
{
    public async Task<GetAllPermissionsResponse> Handle(GetAllPermissionsRequest request, CancellationToken ct)
    {
        var rolePermissions = await db.RolePermissions
            .Select(x => new RolePermissionDto(x.RoleId, x.Type))
            .ToListAsync(ct);

        var userPermissions = await db.UserPermissions
            .Select(x => new UserPermissionDto(x.UserId, x.Type))
            .ToListAsync(ct);

        return new GetAllPermissionsResponse(rolePermissions, userPermissions);
    }
}

using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

public sealed record AddPermissionForRoleRequest(ulong DiscordRoleId, PermissionType Permission) : IRequest<ServiceResponse>;
public sealed record AddPermissionForUserRequest(ulong DiscordUserId, PermissionType Permission) : IRequest<ServiceResponse>;

public class AddPermissionHandler(AccordContext db, IMediator mediator) : IRequestHandler<AddPermissionForUserRequest, ServiceResponse>, IRequestHandler<AddPermissionForRoleRequest, ServiceResponse>
{

    public async Task<ServiceResponse> Handle(AddPermissionForUserRequest request, CancellationToken cancellationToken)
    {
        if (await db.UserPermissions.AnyAsync(x => x.UserId == request.DiscordUserId
                                                    && x.Type == request.Permission, cancellationToken: cancellationToken))
        {
            return ServiceResponse.Ok();
        }

        var permissionEntity = new UserPermission
        {
            UserId = request.DiscordUserId,
            Type = request.Permission,
        };

        db.Add(permissionEntity);

        await db.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new PermissionsUpdateNotification(request.DiscordUserId), cancellationToken);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> Handle(AddPermissionForRoleRequest request, CancellationToken cancellationToken)
    {
        if (await db.RolePermissions.AnyAsync(x => x.RoleId == request.DiscordRoleId
                                                    && x.Type == request.Permission, cancellationToken: cancellationToken))
        {
            return ServiceResponse.Ok();
        }

        var permissionEntity = new RolePermission
        {
            RoleId = request.DiscordRoleId,
            Type = request.Permission,
        };

        db.Add(permissionEntity);

        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
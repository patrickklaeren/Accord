using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Permissions;

public sealed record AddPermissionForRoleRequest(ulong DiscordRoleId, PermissionType Permission) : IRequest<ServiceResponse>;
public sealed record AddPermissionForUserRequest(ulong DiscordUserId, PermissionType Permission) : IRequest<ServiceResponse>;

public class AddPermissionHandler : IRequestHandler<AddPermissionForUserRequest, ServiceResponse>, IRequestHandler<AddPermissionForRoleRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public AddPermissionHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(AddPermissionForUserRequest request, CancellationToken cancellationToken)
    {
        if (await _db.UserPermissions.AnyAsync(x => x.UserId == request.DiscordUserId
                                                    && x.Type == request.Permission, cancellationToken: cancellationToken))
        {
            return ServiceResponse.Ok();
        }

        var permissionEntity = new UserPermission
        {
            UserId = request.DiscordUserId,
            Type = request.Permission,
        };

        _db.Add(permissionEntity);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new PermissionsUpdateNotification(request.DiscordUserId), cancellationToken);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> Handle(AddPermissionForRoleRequest request, CancellationToken cancellationToken)
    {
        if (await _db.RolePermissions.AnyAsync(x => x.RoleId == request.DiscordRoleId
                                                    && x.Type == request.Permission, cancellationToken: cancellationToken))
        {
            return ServiceResponse.Ok();
        }

        var permissionEntity = new RolePermission
        {
            RoleId = request.DiscordRoleId,
            Type = request.Permission,
        };

        _db.Add(permissionEntity);

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
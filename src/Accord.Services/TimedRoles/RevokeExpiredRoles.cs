using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.TimedRoles;

public sealed record RevokeExpiredRolesRequest : IRequest;

public class RevokeExpiredRolesHandler(TimedRoleService timedRoleService) : IRequestHandler<RevokeExpiredRolesRequest>
{
    public async Task Handle(RevokeExpiredRolesRequest request, CancellationToken cancellationToken)
    {
        await timedRoleService.RevokeExpiredRoles(cancellationToken);
    }
}

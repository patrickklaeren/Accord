using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.TimedRoles;

public sealed record RoleChallenge(ulong DiscordRoleId, string Keyword);

public sealed record StartRoleChallengeRequest(ulong DiscordRoleId) : IRequest<ServiceResponse<RoleChallenge>>;

public class StartRoleChallengeHandler(TimedRoleService timedRoleService)
    : IRequestHandler<StartRoleChallengeRequest, ServiceResponse<RoleChallenge>>
{
    public async Task<ServiceResponse<RoleChallenge>> Handle(StartRoleChallengeRequest request, CancellationToken cancellationToken)
    {
        return await timedRoleService.StartChallenge(request.DiscordRoleId, cancellationToken);
    }
}

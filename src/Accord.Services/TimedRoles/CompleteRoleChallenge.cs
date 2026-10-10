using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.TimedRoles;

public sealed record CompleteRoleChallengeRequest(ulong DiscordUserId, ulong DiscordRoleId,
    string Keyword, string Answer) : IRequest<ServiceResponse>;

public class CompleteRoleChallengeHandler(TimedRoleService timedRoleService)
    : IRequestHandler<CompleteRoleChallengeRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(CompleteRoleChallengeRequest request, CancellationToken cancellationToken)
    {
        return await timedRoleService.CompleteChallenge(request.DiscordUserId, request.DiscordRoleId,
            request.Keyword, request.Answer, cancellationToken);
    }
}

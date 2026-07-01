using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record EnsureUserExistsRequest(ulong DiscordUserId) : IRequest;

internal class EnsureUserExistsHandler(UserService userService) : IRequestHandler<EnsureUserExistsRequest>
{
    public async Task Handle(EnsureUserExistsRequest request, CancellationToken cancellationToken)
    {
        await userService.EnsureUserExists(request.DiscordUserId, cancellationToken);
    }
}
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record EnsureUserExistsRequest(ulong DiscordUserId) : IRequest;

public class EnsureUserExistsHandler(UserService userService) : IRequestHandler<EnsureUserExistsRequest>
{
    public async Task Handle(EnsureUserExistsRequest request, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(request.DiscordUserId, cancellationToken);

        if (userExists)
            return;
        
        await userService.AddUser(request.DiscordUserId, cancellationToken);
    }
}
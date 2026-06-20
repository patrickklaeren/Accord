using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record GetUserProfileRequest(ulong DiscordUserId) : IRequest<GetUserProfileDto?>;

public class GetUserProfileHandler(UserProfileService userProfileService) 
    : IRequestHandler<GetUserProfileRequest, GetUserProfileDto?>
{
    public async Task<GetUserProfileDto?> Handle(GetUserProfileRequest request, CancellationToken cancellationToken)
    {
        return await userProfileService.GetProfile(request.DiscordUserId, cancellationToken);
    }
}
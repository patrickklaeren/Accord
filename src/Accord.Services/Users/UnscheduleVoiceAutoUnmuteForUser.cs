using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record UnscheduleVoiceAutoUnmuteForUserRequest(ulong DiscordUserId) : INotification;

internal class UnscheduleVoiceAutoUnmuteForUserHandler(UserService userService) : INotificationHandler<UnscheduleVoiceAutoUnmuteForUserRequest>
{
    public async Task Handle(UnscheduleVoiceAutoUnmuteForUserRequest request, CancellationToken cancellationToken)
    {
        await userService.UnscheduleVoiceAutoUnmuteForUser(request.DiscordUserId, cancellationToken);
    }
}
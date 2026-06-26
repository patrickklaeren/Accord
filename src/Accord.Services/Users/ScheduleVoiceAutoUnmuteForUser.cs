using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record ScheduleVoiceAutoUnmuteForUserRequest(ulong DiscordUserId) : INotification;

internal class ScheduleVoiceAutoUnmuteForUserHandler(UserService userService) : INotificationHandler<ScheduleVoiceAutoUnmuteForUserRequest>
{
    public async Task Handle(ScheduleVoiceAutoUnmuteForUserRequest request, CancellationToken cancellationToken)
    {
        await userService.ScheduleVoiceAutoUnmuteForUser(request.DiscordUserId, cancellationToken);
    }
}
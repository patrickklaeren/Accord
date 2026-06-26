using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Users;

public sealed record ScheduleVoiceAutoUnmuteForUserRequest(ulong DiscordUserId) : IRequest<DateTimeOffset?>;

internal class ScheduleVoiceAutoUnmuteForUserHandler(UserService userService) : IRequestHandler<ScheduleVoiceAutoUnmuteForUserRequest, DateTimeOffset?>
{
    public async Task<DateTimeOffset?> Handle(ScheduleVoiceAutoUnmuteForUserRequest request, CancellationToken cancellationToken)
    {
        return await userService.ScheduleVoiceAutoUnmuteForUser(request.DiscordUserId, cancellationToken);
    }
}
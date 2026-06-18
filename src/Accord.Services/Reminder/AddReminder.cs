using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.Reminder;

public sealed record AddReminderRequest(ulong DiscordUserId, ulong DiscordChannelId, TimeSpan TimeSpan, string Message) : IRequest<ServiceResponse>;

public class AddReminderHandler(UserService userService, UserReminderService userReminderService) : IRequestHandler<AddReminderRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(AddReminderRequest request, CancellationToken cancellationToken)
    {
        var userExists = await userService.UserExists(request.DiscordUserId, cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail<GetUserDto>("User does not exist");

        await userReminderService.AddReminder(request.DiscordUserId,
            request.DiscordChannelId,
            request.TimeSpan,
            request.Message,
            cancellationToken);
        
        return ServiceResponse.Ok();
    }
}
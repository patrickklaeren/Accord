using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Reminder;

public sealed record DeleteReminderRequest(ulong DiscordUserId, int ReminderId) : IRequest;

public sealed record DeleteAllRemindersRequest(ulong DiscordUserId) : IRequest;

public class DeleteReminderHandler(UserReminderService userReminderService) :
    IRequestHandler<DeleteReminderRequest>,
    IRequestHandler<DeleteAllRemindersRequest>
{
    public async Task Handle(DeleteReminderRequest request, CancellationToken cancellationToken)
    {
        await userReminderService.DeleteReminder(request.ReminderId, request.DiscordUserId, cancellationToken);
    }

    public async Task Handle(DeleteAllRemindersRequest request, CancellationToken cancellationToken)
    {
        await userReminderService.DeleteAllRemindersForUser(request.DiscordUserId, cancellationToken);
    }
}
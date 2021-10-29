using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Reminder;

public sealed record DeleteReminderRequest(ulong DiscordUserId, int ReminderId) : IRequest<ServiceResponse>;

public sealed record DeleteAllRemindersRequest(ulong DiscordUserId) : IRequest<ServiceResponse>;

public class DeleteReminderHandler :
    IRequestHandler<DeleteReminderRequest, ServiceResponse>,
    IRequestHandler<DeleteAllRemindersRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public DeleteReminderHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(DeleteReminderRequest request, CancellationToken cancellationToken)
    {
        var hasReminder = await _mediator.Send(new UserHasReminderRequest(request.DiscordUserId, request.ReminderId), cancellationToken);
        if (!hasReminder.Success || !hasReminder.Value)
        {
            return ServiceResponse.Fail("Reminder does not exist");
        }

        var reminder = await _db.UserReminders.SingleAsync(
            x => x.UserId == request.DiscordUserId && x.Id == request.ReminderId,
            cancellationToken);


        _db.Remove(reminder);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateGetRemindersRequest(request.DiscordUserId), cancellationToken);

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> Handle(DeleteAllRemindersRequest request, CancellationToken cancellationToken)
    {
        var reminds = await _db.UserReminders
            .Where(x => x.UserId == request.DiscordUserId)
            .ToListAsync(cancellationToken);

        _db.RemoveRange(reminds);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateGetRemindersRequest(request.DiscordUserId), cancellationToken);

        return ServiceResponse.Ok();
    }
}
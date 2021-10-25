using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.Reminder;

public sealed record AddReminderRequest(ulong DiscordUserId, ulong DiscordChannelId, TimeSpan TimeSpan, String Message) : IRequest<ServiceResponse>;
public class AddReminderHandler : IRequestHandler<AddReminderRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public AddReminderHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
            
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(AddReminderRequest request, CancellationToken cancellationToken)
    {
        var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail<GetUserDto>("User does not exist");

        var dateTime = DateTimeOffset.Now;
            
        var reminder = new UserReminder
        {
            UserId = request.DiscordUserId,
            DiscordChannelId = request.DiscordChannelId,
            RemindAt = dateTime.Add(request.TimeSpan),
            CreatedAt = dateTime,
            Message = request.Message
        };

        _db.Add(reminder);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateGetRemindersRequest(request.DiscordUserId), cancellationToken);
            
        return ServiceResponse.Ok();
    }
}
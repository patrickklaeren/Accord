using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Reminder;

public sealed record GetRemindersRequest(ulong DiscordUserId) : IRequest<IReadOnlyCollection<UserReminder>>;

public sealed record GetRemindersToNotifyRequest : IRequest<IReadOnlyCollection<UserReminder>>;

public class GetRemindersHandler(UserReminderService userReminderService) : 
    IRequestHandler<GetRemindersRequest, IReadOnlyCollection<UserReminder>>, 
    IRequestHandler<GetRemindersToNotifyRequest, IReadOnlyCollection<UserReminder>>
{
    public async Task<IReadOnlyCollection<UserReminder>> Handle(GetRemindersRequest request, CancellationToken cancellationToken)
    {
        return await userReminderService.GetAllRemindersForUser(request.DiscordUserId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserReminder>> Handle(GetRemindersToNotifyRequest toNotifyRequest, CancellationToken cancellationToken)
    {
        return await userReminderService.GetRemindersToNotify(cancellationToken);
    }
}
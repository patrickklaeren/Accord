using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Reminder;

[RegisterScoped]
public class UserReminderService(AccordContext db)
{
    public async Task<IReadOnlyCollection<UserReminder>> GetAllRemindersForUser(ulong discordUserId, CancellationToken cancellationToken)
    {
        return await db
            .UserReminders
            .AsNoTracking()
            .Where(x=>x.UserId == discordUserId)
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<IReadOnlyCollection<UserReminder>> GetRemindersToNotify(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        
        return await db
            .UserReminders
            .AsNoTracking()
            .Where(x => x.RemindAt <= now)
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    public async Task AddReminder(ulong discordUserId,
        ulong discordChannelId,
        TimeSpan timeUntilReminder,
        string reminder,
        CancellationToken cancellationToken)
    {
        var dateTime = DateTimeOffset.UtcNow;
        
        var reminderEntity = new UserReminder
        {
            UserId = discordUserId,
            DiscordChannelId = discordChannelId,
            RemindAt = dateTime.Add(timeUntilReminder),
            CreatedAt = dateTime,
            Message = reminder
        };

        db.Add(reminderEntity);
        await db.SaveChangesAsync(cancellationToken);

        ServiceResponse.Ok();
    }
    
    public async Task DeleteReminder(int reminderId, ulong discordUserId, CancellationToken cancellationToken)
    {
        await db.UserReminders
            .Where(x => x.Id == reminderId)
            .Where(x => x.UserId == discordUserId)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteAllRemindersForUser(ulong discordUserId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        
        await db.UserReminders
            .Where(x => x.UserId == discordUserId)
            .Where(x => x.RemindAt > now)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }
}
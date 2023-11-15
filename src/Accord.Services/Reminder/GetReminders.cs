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

public sealed record GetRemindersRequest(ulong DiscordUserId) : IRequest<ServiceResponse<List<UserReminder>>>;

public sealed record GetAllRemindersRequest : IRequest<ServiceResponse<List<UserReminder>>>;

public sealed record GetReminderRequest(ulong DiscordUserId, int ReminderId) : IRequest<ServiceResponse<UserReminder>>;

public sealed record UserHasReminderRequest(ulong DiscordUserId, int ReminderId) : IRequest<ServiceResponse<bool>>;

public sealed record InvalidateGetRemindersRequest(ulong DiscordUserId) : IRequest;

[AutoConstructor]
public partial class GetRemindersHandler : 
    IRequestHandler<InvalidateGetRemindersRequest>, 
    IRequestHandler<GetReminderRequest, ServiceResponse<UserReminder>>, 
    IRequestHandler<UserHasReminderRequest, ServiceResponse<bool>>, 
    IRequestHandler<GetRemindersRequest, ServiceResponse<List<UserReminder>>>, 
    IRequestHandler<GetAllRemindersRequest, ServiceResponse<List<UserReminder>>>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<ServiceResponse<List<UserReminder>>> Handle(GetRemindersRequest request, CancellationToken cancellationToken)
    {
        var result = await _appCache.GetOrAddAsync(
            BuildGetRemindersWithId(request.DiscordUserId), 
            () => GetRemindersByUserId(request.DiscordUserId), 
            DateTimeOffset.Now.AddDays(30)
        );
            
        return ServiceResponse.Ok(result);
    }

    public async Task<ServiceResponse<List<UserReminder>>> Handle(GetAllRemindersRequest request, CancellationToken cancellationToken)
    {
        var result = await _appCache.GetOrAddAsync(
            BuildGetReminders(), 
            () => GetReminders(), 
            DateTimeOffset.Now.AddDays(30)
        );
            
        return ServiceResponse.Ok(result);
    }

    public async Task<ServiceResponse<UserReminder>> Handle(GetReminderRequest request, CancellationToken cancellationToken)
    {
        var result = await _appCache.GetOrAddAsync(
            BuildGetRemindersWithId(request.DiscordUserId), 
            () => GetRemindersByUserId(request.DiscordUserId), 
            DateTimeOffset.Now.AddDays(30)
        );
        try
        {
            return ServiceResponse.Ok(result.Single(x => x.Id == request.ReminderId));
        }
        catch (Exception)
        {
            return ServiceResponse.Fail<UserReminder>($"User doesn't have a reminder with id: {request.ReminderId}");
        }
    }
    public async Task<ServiceResponse<bool>> Handle(UserHasReminderRequest request, CancellationToken cancellationToken)
    {
        var result = await _appCache.GetOrAddAsync(
            BuildGetRemindersWithId(request.DiscordUserId), 
            () => GetRemindersByUserId(request.DiscordUserId), 
            DateTimeOffset.Now.AddDays(30)
        );
            
        return ServiceResponse.Ok(result.Any(x => x.Id == request.ReminderId));
    }
    
    private async Task<List<UserReminder>> GetReminders() => 
        await _db.UserReminders.ToListAsync();

    private async Task<List<UserReminder>> GetRemindersByUserId(ulong userId) => 
        await _db.UserReminders.Where(x=>x.UserId == userId).ToListAsync();

    private static string BuildGetReminders() => 
        $"{nameof(GetRemindersHandler)}/{nameof(GetReminders)}";

    private static string BuildGetRemindersWithId(ulong discordUserId) => 
        $"{nameof(GetRemindersHandler)}/{nameof(GetRemindersByUserId)}/{discordUserId}";

    public Task Handle(InvalidateGetRemindersRequest request, CancellationToken cancellationToken)
    {
        _appCache.Remove(BuildGetRemindersWithId(request.DiscordUserId));
        _appCache.Remove(BuildGetReminders());
        return Task.CompletedTask;
    }
}
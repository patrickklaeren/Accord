using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Reminder;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.HostedServices;

[AutoConstructor]
public partial class RemindersHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var mediator = services.GetRequiredService<IMediator>();

        var channelApi = services.GetRequiredService<IDiscordRestChannelAPI>();

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessReminders(mediator, channelApi, stoppingToken);
        }
    }

    private async Task ProcessReminders(IMediator mediator, IDiscordRestChannelAPI channelApi, CancellationToken stoppingToken)
    {
        var reminders = await mediator.Send(new GetAllRemindersRequest(), stoppingToken);

        var processableReminders = reminders.Value!.Where(x => x.RemindAt <= DateTime.Now);

        foreach (var reminder in processableReminders)
        {
            if (DateTime.Now - reminder.RemindAt < TimeSpan.FromMinutes(1))
            {
                var embed = new Embed
                {
                    Title = "Reminder",
                    Description = reminder.Message
                };

                await channelApi.CreateMessageAsync(new Snowflake(reminder.DiscordChannelId), DiscordFormatter.UserIdToMention(reminder.UserId), embeds: new[] { embed }, ct: stoppingToken);
            }

            await mediator.Send(new DeleteReminderRequest(reminder.UserId, reminder.Id), stoppingToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }
}
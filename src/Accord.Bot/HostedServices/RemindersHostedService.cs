using System;
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

public class RemindersHostedService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var mediator = services.GetRequiredService<IMediator>();

        var channelApi = services.GetRequiredService<IDiscordRestChannelAPI>();

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessReminders(mediator, channelApi, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessReminders(IMediator mediator, 
        IDiscordRestChannelAPI channelApi, 
        CancellationToken stoppingToken)
    {
        var reminders = await mediator.Send(new GetRemindersToNotifyRequest(), stoppingToken);

        foreach (var reminder in reminders)
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
    }
}
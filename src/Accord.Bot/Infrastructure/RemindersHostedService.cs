using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Reminder;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.Infrastructure
{
    public class RemindersHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RemindersHostedService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var mediator = services.GetRequiredService<IMediator>();

            var channelApi = services.GetRequiredService<IDiscordRestChannelAPI>();

            await ProcessReminders(mediator, channelApi, true, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessReminders(mediator, channelApi, false, stoppingToken);
            }
        }

        private async Task ProcessReminders(IMediator mediator, IDiscordRestChannelAPI channelApi, bool discard, CancellationToken stoppingToken)
        {
            var reminders = await mediator.Send(new GetAllRemindersRequest(), stoppingToken);

            var processableReminders = reminders.Value!.Where(x => x.RemindAt <= DateTime.Now);

            foreach (var reminder in processableReminders)
            {
                if ((discard && (DateTime.Now - reminder.RemindAt) < TimeSpan.FromMinutes(1)) || !discard)
                {
                    var embed = new Embed
                    {
                        Title = "Reminder",
                        Description = reminder.Message
                    };

                    await channelApi.CreateMessageAsync(new Snowflake(reminder.DiscordChannelId), $"<@{reminder.UserId}>", embeds: new List<IEmbed>{embed}, ct: stoppingToken);
                }

                await mediator.Send(new DeleteReminderRequest(reminder.UserId, reminder.Id), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
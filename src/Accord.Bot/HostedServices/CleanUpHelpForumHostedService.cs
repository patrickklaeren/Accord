using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Infrastructure;
using Accord.Bot.RequestHandlers;
using Accord.Services.Helpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Serilog;

namespace Accord.Bot.HostedServices;

public class CleanUpHelpForumHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DiscordConfiguration _discordConfiguration;

    public CleanUpHelpForumHostedService(IServiceScopeFactory serviceScopeFactory, 
        IOptions<DiscordConfiguration> discordConfiguration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _discordConfiguration = discordConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var guildApi = services.GetRequiredService<IDiscordRestGuildAPI>();
        var channelApi = services.GetRequiredService<IDiscordRestChannelAPI>();
        var mediator = services.GetRequiredService<IMediator>();

        var guildSnowflake = new Snowflake(_discordConfiguration.GuildId);
        var helpForumSnowflake = new Snowflake(_discordConfiguration.HelpForumChannelId);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanUpHelpForum(guildSnowflake, helpForumSnowflake, mediator, guildApi, channelApi, stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task CleanUpHelpForum(Snowflake guildId, Snowflake helpForumId, 
        IMediator mediator, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi, 
        CancellationToken stoppingToken)
    {
        var processingAt = DateTimeOffset.Now;
        const int NUMBER_OF_HOURS_TO_GO_BACK = 24;
        var cutOff = processingAt.AddHours(-NUMBER_OF_HOURS_TO_GO_BACK);

        var threads = await guildApi.ListActiveGuildThreadsAsync(guildId, stoppingToken);

        if (!threads.IsSuccess)
            return;

        foreach(var thread in threads.Entity.Threads)
        {
            try
            {
                if (thread.ParentID != helpForumId)
                {
                    continue;
                }

                var lastMessageSentAt = DiscordSnowflakeHelper.ToDateTimeOffset(thread.LastMessageID.Value!.Value.Value);

                if (lastMessageSentAt > cutOff)
                {
                    continue;
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (thread.MemberCount.Value > 1)
                {
                    await channelApi.CreateMessageAsync(thread.ID,
                        "Was this issue resolved? If so, run `/close` - otherwise I will mark this as stale and this post will be archived until there is new activity.",
                        ct: stoppingToken);
                }
                else
                {
                    await channelApi.CreateMessageAsync(thread.ID,
                        "Looks like nothing has happened here. I will mark this as stale and this post will be archived until there is new activity.",
                        ct: stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

                await mediator.Send(new DeactivateForumPostRequest(thread), stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed cleaning up thread {ThreadId}", thread.ID);
            }
        }
    }
}
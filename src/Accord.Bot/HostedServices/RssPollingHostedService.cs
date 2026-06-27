using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Rss;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.HostedServices;

public class RssPollingHostedService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var services = scope.ServiceProvider;

            var mediator = services.GetRequiredService<IMediator>();
            var channelApi = services.GetRequiredService<IDiscordRestChannelAPI>();

            var feedIds = await mediator.Send(new GetFeedIdsToReadRequest(), stoppingToken);

            foreach (var feedId in feedIds)
            {
                var result = await mediator.Send(new GetNewPostsFromFeedRequest(feedId), stoppingToken);

                foreach (var post in result.NewPosts)
                {
                    await channelApi.CreateMessageAsync(new Snowflake(result.DiscordChannelId),
                        $"**{post.Title}**{Environment.NewLine}{post.Url}",
                        ct: stoppingToken);
                }
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

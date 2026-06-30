using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Users;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Services.DemocraticDownVoting;

public class DemocraticDownVotingEventQueueProcessor(ILogger<DemocraticDownVotingEventQueueProcessor> logger, 
    IServiceScopeFactory serviceScopeFactory, 
    DemocraticDownVotingEventQueue eventQueue) 
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var queuedItem =
                await eventQueue.Dequeue(stoppingToken);

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var services = scope.ServiceProvider;

                var mediator = services.GetRequiredService<IMediator>();

                if (queuedItem is IEnsureUserExistsRequest userEvent)
                {
                    await mediator.Send(new EnsureUserExistsRequest(userEvent.DiscordUserId), stoppingToken);
                }

                await mediator.Send(queuedItem, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error occurred executing {queuedItem}", nameof(queuedItem));
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starboard event queue processor is stopping.");
        await base.StopAsync(stoppingToken);
    }
}
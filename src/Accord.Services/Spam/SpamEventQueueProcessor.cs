using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Services.Spam;

public class SpamEventQueueProcessor(ILogger<SpamEventQueueProcessor> logger,
    IServiceScopeFactory serviceScopeFactory,
    SpamEventQueue eventQueue)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var queuedItem = await eventQueue.Dequeue(stoppingToken);

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var services = scope.ServiceProvider;

                var mediator = services.GetRequiredService<IMediator>();
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
        logger.LogInformation("Spam event queue processor is stopping.");
        await base.StopAsync(stoppingToken);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Users;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Services;

[AutoConstructor]
public partial class EventQueueProcessor : BackgroundService
{
    private readonly ILogger<EventQueueProcessor> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEventQueue _eventQueue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var queuedItem =
                await _eventQueue.Dequeue(stoppingToken);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
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
                _logger.LogError(ex,
                    "Error occurred executing {queuedItem}", nameof(queuedItem));
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event queue processor is stopping.");
        await base.StopAsync(stoppingToken);
    }
}
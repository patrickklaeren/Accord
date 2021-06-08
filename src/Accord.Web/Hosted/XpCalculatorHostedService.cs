using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Web.Hosted
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IXpCalculatorQueueService _xpCalculatorQueueService;

        public QueuedHostedService(ILogger<QueuedHostedService> logger, 
            IServiceScopeFactory serviceScopeFactory, 
            IXpCalculatorQueueService xpCalculatorQueueService)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _xpCalculatorQueueService = xpCalculatorQueueService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var queuedItem =
                    await _xpCalculatorQueueService.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();

                    var service = scope.ServiceProvider.GetRequiredService<XpService>();

                    await service.CalculateXp(queuedItem.DiscordUserId, queuedItem.DiscordChannelId, 
                        queuedItem.MessageSentDateTime, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error occurred executing {WorkItem}.", nameof(queuedItem));
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}

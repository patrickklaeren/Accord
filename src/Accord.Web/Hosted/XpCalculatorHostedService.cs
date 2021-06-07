using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services;
using Microsoft.EntityFrameworkCore;
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

                    await using var db = scope.ServiceProvider.GetRequiredService<AccordContext>();

                    var user = await db.Users.SingleOrDefaultAsync(x => x.Id == queuedItem.DiscordUserId, cancellationToken: stoppingToken);

                    if (user is null)
                    {
                        user = new User
                        {
                            Id = queuedItem.DiscordUserId,
                            FirstSeenDateTime = DateTimeOffset.Now,
                        };

                        db.Add(user);
                    }

                    if (user.LastSeenDateTime.AddSeconds(10) > queuedItem.MessageSentDateTime)
                    {
                        user.Xp += 5;
                    }

                    user.LastSeenDateTime = queuedItem.MessageSentDateTime;

                    await db.SaveChangesAsync(stoppingToken);
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

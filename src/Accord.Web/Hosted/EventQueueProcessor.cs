using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Web.Hosted
{
    public class EventQueueProcessor : BackgroundService
    {
        private readonly ILogger<EventQueueProcessor> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IEventQueue _eventQueue;

        public EventQueueProcessor(ILogger<EventQueueProcessor> logger, 
            IServiceScopeFactory serviceScopeFactory, 
            IEventQueue eventQueue)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _eventQueue = eventQueue;
        }

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

                    await services.GetRequiredService<UserService>().EnsureUserExists(queuedItem.DiscordUserId);

                    var action = queuedItem switch
                    {
                        RaidCalculationEvent raidCalculation
                            => services.GetRequiredService<RaidModeService>().Process(raidCalculation.QueuedDateTime),

                        RaidCalculationEvent raidCalculation
                            => services.GetRequiredService<RaidModeService>().Process(raidCalculation.QueuedDateTime),

                        MessageSentEvent messageSent
                            => services.GetRequiredService<XpService>().AddXpForMessage(messageSent.DiscordUserId, messageSent.DiscordChannelId, messageSent.QueuedDateTime, stoppingToken),

                        VoiceConnectedEvent voiceConnected
                            => services.GetRequiredService<VoiceSessionService>().Start(voiceConnected.DiscordUserId, voiceConnected.DiscordChannelId, voiceConnected.DiscordSessionId, voiceConnected.QueuedDateTime),

                        VoiceDisconnectedEvent voiceDisconnected
                            => services.GetRequiredService<VoiceSessionService>().Finish(voiceDisconnected.DiscordSessionId, voiceDisconnected.QueuedDateTime),

                        _ => throw new ArgumentOutOfRangeException(nameof(queuedItem))
                    };

                    await action;
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
}

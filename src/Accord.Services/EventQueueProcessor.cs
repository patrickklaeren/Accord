using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Moderation;
using Accord.Services.Raid;
using Accord.Services.UserMessages;
using Accord.Services.Users;
using Accord.Services.VoiceSessions;
using Accord.Services.Xp;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Services
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

                    var mediator = services.GetRequiredService<IMediator>();

                    if (queuedItem is UserJoinedEvent userJoined)
                    {
                        await mediator.Send(new AddUserRequest(userJoined.DiscordGuildId, userJoined.DiscordUserId, userJoined.DiscordUsername, 
                            userJoined.DiscordDiscriminator, userJoined.DiscordNickname, userJoined.QueuedDateTime), stoppingToken);
                    }
                    else
                    {
                        if (queuedItem is IUserEvent userEvent)
                        {
                            await mediator.Send(new EnsureUserExistsRequest(userEvent.DiscordUserId), stoppingToken);
                        }

                        IRequest<ServiceResponse> action = queuedItem switch
                        {
                            RaidCalculationEvent raidCalculation
                                => new RaidCalculationRequest(raidCalculation.DiscordGuildId, 
                                    new GuildUserDto(raidCalculation.DiscordUserId, 
                                        raidCalculation.DiscordUsername, 
                                        raidCalculation.DiscordUserDiscriminator, 
                                        null,
                                        raidCalculation.QueuedDateTime)),

                            AddMessageEvent addMessage
                                => new AddMessageRequest(addMessage.DiscordMessageId, addMessage.DiscordUserId, 
                                    addMessage.DiscordChannelId, addMessage.QueuedDateTime),

                            DeleteMessageEvent addMessage
                                => new DeleteMessageRequest(addMessage.DiscordMessageId),

                            CalculateXpForUserEvent calculateXp
                                => new AddXpForMessageRequest(calculateXp.DiscordUserId, 
                                    calculateXp.DiscordChannelId, calculateXp.QueuedDateTime),

                            VoiceConnectedEvent voiceConnected
                                => new StartVoiceSessionRequest(voiceConnected.DiscordGuildId, 
                                    voiceConnected.DiscordUserId, voiceConnected.DiscordChannelId, 
                                    voiceConnected.DiscordSessionId, voiceConnected.QueuedDateTime),

                            VoiceDisconnectedEvent voiceDisconnected
                                => new FinishVoiceSessionRequest(voiceDisconnected.DiscordGuildId, 
                                    voiceDisconnected.DiscordSessionId, voiceDisconnected.QueuedDateTime),

                            _ => throw new ArgumentOutOfRangeException(nameof(queuedItem))
                        };

                        await mediator.Send(action, stoppingToken);
                    }
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

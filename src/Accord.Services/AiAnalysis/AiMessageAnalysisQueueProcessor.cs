using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Services.AiAnalysis;

public sealed class AiMessageAnalysisQueueProcessor(
    ILogger<AiMessageAnalysisQueueProcessor> logger,
    IServiceScopeFactory serviceScopeFactory,
    AiMessageAnalysisQueue eventQueue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            AnalyseMemberMessageRequest? request = null;
            try
            {
                request = await eventQueue.Dequeue(stoppingToken);
                using var scope = serviceScopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var analysis = await mediator.Send(request, stoppingToken);
                if (analysis is not null)
                {
                    logger.LogInformation(
                        "AI categorised message {DiscordMessageId} from member {DiscordUserId} in channel {DiscordChannelId} as {Category}",
                        request.DiscordMessageId, request.DiscordUserId, request.DiscordChannelId, analysis.Category);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "AI analysis skipped for message {DiscordMessageId}", request?.DiscordMessageId);
            }
        }
    }
}

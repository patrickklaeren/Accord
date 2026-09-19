using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.VoiceChannelLeasing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Accord.Bot.HostedServices;

public class VoiceChannelLeaseCleanupHostedService(
    IServiceScopeFactory serviceScopeFactory,
    VoiceChannelLeaseOccupancyTracker occupancyTracker) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);

    private static readonly TimeSpan CreationGracePeriod = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanUpEmptyLeases(stoppingToken);
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task CleanUpEmptyLeases(CancellationToken stoppingToken)
    {
        if (!occupancyTracker.HasSeeded)
            return;

        using var scope = serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var leases = await mediator.Send(new GetActiveVoiceChannelLeasesRequest(), stoppingToken);

        foreach (var lease in leases)
        {
            if (!occupancyTracker.IsChannelEmpty(lease.DiscordChannelId))
                continue;

            var isWithinCreationGrace = DateTimeOffset.UtcNow - lease.CreatedDateTime < CreationGracePeriod;

            if (isWithinCreationGrace && !occupancyTracker.HasEverBeenOccupied(lease.DiscordChannelId))
                continue;

            await mediator.Send(
                new CloseVoiceChannelLeaseRequest(lease.Id, null),
                stoppingToken);
        }
    }
}

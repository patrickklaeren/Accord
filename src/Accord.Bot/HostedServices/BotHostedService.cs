using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Accord.Bot.HostedServices;

public class BotHostedService(BotClient botClient) : IHostedService
{
    private Task? _runTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runTask = botClient.Run(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _runTask ?? Task.CompletedTask;
    }
}
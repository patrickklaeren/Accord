using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Accord.Bot.HostedServices;

[AutoConstructor]
public partial class BotHostedService : IHostedService
{
    private readonly BotClient _botClient;
    private Task? _runTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runTask = _botClient.Run(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _runTask ?? Task.CompletedTask;
    }
}
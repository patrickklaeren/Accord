using System.Threading;
using System.Threading.Tasks;
using Accord.Bot;
using Microsoft.Extensions.Hosting;

namespace Accord.Web.Hosted
{
    public class BotHostedService : IHostedService
    {
        private readonly BotClient _botClient;
        private Task? _runTask;

        public BotHostedService(BotClient botClient)
        {
            _botClient = botClient;
        }

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
}

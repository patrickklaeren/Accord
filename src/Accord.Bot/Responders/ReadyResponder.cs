using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class ReadyResponder : IResponder<IReady>
    {
        private readonly DiscordGatewayClient _discordGatewayClient;

        public ReadyResponder(DiscordGatewayClient discordGatewayClient)
        {
            _discordGatewayClient = discordGatewayClient;
        }

        public Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
        {
            var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
            {
                new Activity("for script kiddies", ActivityType.Watching)
            });

            _discordGatewayClient.SubmitCommandAsync(updateCommand);

            return Task.FromResult(Result.FromSuccess());
        }
    }
}

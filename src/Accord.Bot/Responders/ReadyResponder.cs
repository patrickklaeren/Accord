using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
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
        private readonly DiscordCache _discordCache;

        public ReadyResponder(DiscordGatewayClient discordGatewayClient, DiscordCache discordCache)
        {
            _discordGatewayClient = discordGatewayClient;
            _discordCache = discordCache;
        }

        public Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
        {
            _discordCache.SetSelfSnowflake(gatewayEvent.User.ID);

            var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
            {
                new Activity("for script kiddies", ActivityType.Watching)
            });

            _discordGatewayClient.SubmitCommandAsync(updateCommand);

            return Task.FromResult(Result.FromSuccess());
        }
    }
}

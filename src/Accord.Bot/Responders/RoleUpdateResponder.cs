using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class RoleUpdateResponder :
        IResponder<IGuildRoleUpdate>,
        IResponder<IGuildRoleDelete>,
        IResponder<IGuildRoleCreate>
    {
        private readonly DiscordCache _discordCache;

        public RoleUpdateResponder(DiscordCache discordCache)
        {
            _discordCache = discordCache;
        }

        public Task<Result> RespondAsync(IGuildRoleUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var guildRoles = _discordCache.GetGuildRoles(gatewayEvent.GuildID).Where(x => x.ID != gatewayEvent.Role.ID).ToList();
            guildRoles.Add(gatewayEvent.Role);
            _discordCache.SetGuildRoles(gatewayEvent.GuildID, guildRoles);

            if (gatewayEvent.Role.Name == "@everyone")
            {
                _discordCache.SetEveryoneRole(gatewayEvent.GuildID, gatewayEvent.Role);
            }

            return Task.FromResult(Result.FromSuccess());
        }

        public Task<Result> RespondAsync(IGuildRoleDelete gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var guildRoles = _discordCache.GetGuildRoles(gatewayEvent.GuildID).Where(x => x.ID != gatewayEvent.RoleID).ToList();
            _discordCache.SetGuildRoles(gatewayEvent.GuildID, guildRoles);

            return Task.FromResult(Result.FromSuccess());
        }

        public Task<Result> RespondAsync(IGuildRoleCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var guildRoles = _discordCache.GetGuildRoles(gatewayEvent.GuildID).ToList();
            guildRoles.Add(gatewayEvent.Role);
            _discordCache.SetGuildRoles(gatewayEvent.GuildID, guildRoles);

            return Task.FromResult(Result.FromSuccess());
        }
    }
}
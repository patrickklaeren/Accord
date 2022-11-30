using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

[AutoConstructor]
public partial class RoleUpdateResponder :
    IResponder<IGuildRoleUpdate>,
    IResponder<IGuildRoleDelete>,
    IResponder<IGuildRoleCreate>
{
    private readonly DiscordCache _discordCache;

    public Task<Result> RespondAsync(IGuildRoleUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildRoles();
        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IGuildRoleDelete gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildRoles();
        return Task.FromResult(Result.FromSuccess());
    }

    public Task<Result> RespondAsync(IGuildRoleCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        _discordCache.InvalidateGuildRoles();
        return Task.FromResult(Result.FromSuccess());
    }
}
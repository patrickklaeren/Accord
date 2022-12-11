using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Accord.Services;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Responders;

[AutoConstructor]
public partial class ReadyResponder : IResponder<IReady>
{
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly DiscordCache _discordCache;
    private readonly IDiscordRestGuildAPI _guildApi;

    private readonly DiscordConfiguration _discordConfiguration;

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
    {
        _discordCache.SetSelfSnowflake(gatewayEvent.User.ID);
        await CacheGuild(gatewayEvent.User, ct);

        var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
        {
            new Activity("for everything", ActivityType.Watching)
        });

        _discordGatewayClient.SubmitCommand(updateCommand);

        return Result.FromSuccess();
    }

    private async Task CacheGuild(IUser user, CancellationToken ct = default)
    {
        var guildSnowflake = new Snowflake(_discordConfiguration.GuildId);

        var guildMember = await _guildApi.GetGuildMemberAsync(guildSnowflake, user.ID, ct);

        if (guildMember.IsSuccess)
        {
            _discordCache.SetGuildSelfMember(guildMember.Entity);
        }

        var guild = await _guildApi.GetGuildAsync(guildSnowflake, true, ct: ct);

        if (guild.IsSuccess)
        {
            var everyoneRole = guild.Entity.Roles.Single(x => x.Name == "@everyone");
            _discordCache.SetEveryoneRole(everyoneRole);
        }
    }
}
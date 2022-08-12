using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly DiscordGatewayClient _discordGatewayClient;
    private readonly DiscordCache _discordCache;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly BotState _botState;

    public ReadyResponder(DiscordGatewayClient discordGatewayClient, DiscordCache discordCache, IDiscordRestGuildAPI guildApi, BotState botState)
    {
        _discordGatewayClient = discordGatewayClient;
        _discordCache = discordCache;
        _guildApi = guildApi;
        _botState = botState;
    }

    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new())
    {
        _discordCache.SetSelfSnowflake(gatewayEvent.User.ID);

        var tasks = new List<Task>();
            
        foreach (var unavailableGuild in gatewayEvent.Guilds)
        {
            tasks.Add(CacheGuild(unavailableGuild, gatewayEvent.User, ct));
        }

        await Task.WhenAll(tasks);

        _botState.IsCacheReady = true;

        var updateCommand = new UpdatePresence(ClientStatus.Online, false, null, new IActivity[]
        {
            new Activity("for everything", ActivityType.Watching)
        });

        _discordGatewayClient.SubmitCommand(updateCommand);

        return Result.FromSuccess();
    }

    private async Task CacheGuild(IUnavailableGuild unavailableGuild, IUser user, CancellationToken ct = default)
    {
        var guildMember = await _guildApi.GetGuildMemberAsync(unavailableGuild.ID, user.ID, ct);
        if (guildMember.IsSuccess)
            _discordCache.SetGuildSelfMember(unavailableGuild.ID, guildMember.Entity);

        var guild = await _guildApi.GetGuildAsync(unavailableGuild.ID, true, ct: ct);
            
        if (!guild.IsSuccess)
            return;

        _discordCache.SetGuildRoles(guild.Entity.ID, guild.Entity.Roles);
            
        var channels = (await _guildApi.GetGuildChannelsAsync(guild.Entity.ID, ct)).Entity;

        _discordCache.SetGuildChannels(guild.Entity.ID, channels);

        var everyoneRole = guild.Entity.Roles.Single(x => x.Name == "@everyone");
        _discordCache.SetEveryoneRole(guild.Entity.ID, everyoneRole);
    }
}
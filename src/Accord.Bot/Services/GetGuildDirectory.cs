using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Guilds;
using Accord.Services.Permissions;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.Services;

public sealed class GetGuildDirectoryHandler(DiscordCache cache, IDiscordRestGuildAPI guildApi,
    DiscordConfiguration configuration)
    : IRequestHandler<GetGuildRolesRequest, GuildRolesResponse>,
      IRequestHandler<GetGuildChannelsRequest, GuildChannelsResponse>,
      IRequestHandler<GetGuildAccessRequest, GuildAccessResponse>
{
    public async Task<GuildRolesResponse> Handle(GetGuildRolesRequest request, CancellationToken cancellationToken)
    {
        var roles = await cache.GetGuildRoles();
        return new GuildRolesResponse(roles.Select(role => new GuildRoleDto(role.ID.Value, role.Name)).ToArray());
    }

    public async Task<GuildChannelsResponse> Handle(GetGuildChannelsRequest request, CancellationToken cancellationToken)
    {
        var channels = await cache.GetGuildChannels();
        return new GuildChannelsResponse(channels.Select(channel =>
            new GuildChannelDto(channel.ID.Value, channel.Name.HasValue ? channel.Name.Value ?? $"{channel.ID.Value}" : $"{channel.ID.Value}")).ToArray());
    }

    public async Task<GuildAccessResponse> Handle(GetGuildAccessRequest request, CancellationToken cancellationToken)
    {
        var member = await cache.GetGuildMember(request.DiscordUserId);
        if (!member.IsSuccess)
            return new GuildAccessResponse(null);

        var guild = await guildApi.GetGuildAsync(new Snowflake(configuration.GuildId), ct: cancellationToken);
        if (!guild.IsSuccess)
            return new GuildAccessResponse(null);

        // Every member also has the implicit @everyone role, whose ID is the guild ID.
        var roleIds = member.Entity.Roles.Select(role => role.Value).Append(configuration.GuildId).ToHashSet();
        var roles = await cache.GetGuildRoles();
        var administrator = guild.Entity.OwnerID.Value == request.DiscordUserId ||
            roles.Any(role => roleIds.Contains(role.ID.Value) && role.Permissions.HasPermission(DiscordPermission.Administrator));

        return new GuildAccessResponse(new PermissionUser(request.DiscordUserId, roleIds, administrator, false));
    }
}

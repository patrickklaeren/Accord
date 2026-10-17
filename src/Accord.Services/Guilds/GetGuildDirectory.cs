using System.Collections.Generic;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Guilds;

public sealed record GetGuildRolesRequest : IRequest<GuildRolesResponse>;
public sealed record GuildRolesResponse(IReadOnlyList<GuildRoleDto> Roles);
public sealed record GuildRoleDto(ulong Id, string Name);

public sealed record GetGuildChannelsRequest : IRequest<GuildChannelsResponse>;
public sealed record GuildChannelsResponse(IReadOnlyList<GuildChannelDto> Channels);
public sealed record GuildChannelDto(ulong Id, string Name);

public sealed record GetGuildAccessRequest(ulong DiscordUserId) : IRequest<GuildAccessResponse>;
public sealed record GuildAccessResponse(PermissionUser? User);

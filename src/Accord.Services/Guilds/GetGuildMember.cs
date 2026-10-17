using MediatR;

namespace Accord.Services.Guilds;

public sealed record GetGuildMemberRequest(ulong DiscordUserId) : IRequest<ServiceResponse<DiscordGuildMemberDto>>;

public sealed record DiscordGuildMemberDto(string Username, string? Nickname, string? GuildAvatarHash, ulong[] RoleIds);

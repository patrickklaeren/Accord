using MediatR;

namespace Accord.Services.Moderation;

public sealed record KickRequest(ulong DiscordGuildId, GuildUserDto User, string Reason) : IRequest;
public sealed record BanRequest(ulong DiscordGuildId, GuildUserDto User, string Reason) : IRequest;
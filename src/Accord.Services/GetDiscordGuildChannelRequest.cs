using MediatR;

namespace Accord.Services;

public sealed record GetDiscordGuildChannelRequest(ulong DiscordChannelId) : IRequest<ServiceResponse<DiscordGuildChannelDto>>;
public sealed record DiscordGuildChannelDto(ulong Id, string? Name, ulong? ParentDiscordChannelId, ulong? OwnerDiscordUserId);
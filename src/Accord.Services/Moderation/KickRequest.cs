using MediatR;

namespace Accord.Services.Moderation;

public sealed record KickRequest(ulong DiscordGuildId, ulong DiscordUserId, string DiscordUsername, string Reason) : IRequest;

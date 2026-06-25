using MediatR;

namespace Accord.Services;

public sealed record DeleteDiscordMessageRequest(ulong DiscordMessageId, ulong DiscordChannelId) : IRequest<ServiceResponse>;
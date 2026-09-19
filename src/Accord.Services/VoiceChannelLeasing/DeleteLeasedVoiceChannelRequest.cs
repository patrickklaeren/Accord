using MediatR;

namespace Accord.Services.VoiceChannelLeasing;

public sealed record DeleteLeasedVoiceChannelRequest(ulong DiscordChannelId) : IRequest<ServiceResponse>;

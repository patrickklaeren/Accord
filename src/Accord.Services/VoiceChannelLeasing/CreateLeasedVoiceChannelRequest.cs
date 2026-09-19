using MediatR;

namespace Accord.Services.VoiceChannelLeasing;

public sealed record CreateLeasedVoiceChannelRequest(
    ulong DiscordGuildId,
    string ChannelName,
    int? MaxUsers) : IRequest<ServiceResponse<CreatedVoiceChannelDto>>;

public sealed record CreatedVoiceChannelDto(ulong ChannelId, ulong? ParentCategoryId);

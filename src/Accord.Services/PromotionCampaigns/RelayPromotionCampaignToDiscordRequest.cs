using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record RelayNewPromotionCampaignToDiscordRequest(ulong DiscordChannelId, PromotionCampaignDto Campaign) : IRequest<ulong?>;
public sealed record RelayExistingPromotionCampaignToDiscordRequest(ulong DiscordChannelId, ulong DiscordMessageId, PromotionCampaignDto Campaign) : IRequest;
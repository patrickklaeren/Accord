using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record ApplyPromotionCampaignRoleToDiscordRequest(
    int PromotionCampaignId,
    ulong DiscordUserId,
    ulong DiscordRoleId,
    ulong ApprovedByUserId) : IRequest<bool>;

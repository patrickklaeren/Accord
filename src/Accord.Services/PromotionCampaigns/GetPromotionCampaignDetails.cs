using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record GetPromotionCampaignDetailsRequest(int PromotionCampaignId) : IRequest<PromotionCampaignDetailsDto?>;

internal class GetPromotionCampaignDetailsHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<GetPromotionCampaignDetailsRequest, PromotionCampaignDetailsDto?>
{
    public async Task<PromotionCampaignDetailsDto?> Handle(
        GetPromotionCampaignDetailsRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.GetCampaignDetails(request.PromotionCampaignId, cancellationToken);
    }
}

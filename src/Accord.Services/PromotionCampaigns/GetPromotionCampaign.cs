using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record GetPromotionCampaignRequest(int PromotionCampaignId) : IRequest<PromotionCampaignDto?>;

internal class GetPromotionCampaignHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<GetPromotionCampaignRequest, PromotionCampaignDto?>
{
    public async Task<PromotionCampaignDto?> Handle(
        GetPromotionCampaignRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.GetCampaign(request.PromotionCampaignId, cancellationToken);
    }
}

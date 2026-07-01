using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record ClosePromotionCampaignRequest(int PromotionCampaignId, ulong ClosedByUserId)
    : IRequest<ServiceResponse<PromotionCampaignDto>>;

internal class ClosePromotionCampaignHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<ClosePromotionCampaignRequest, ServiceResponse<PromotionCampaignDto>>
{
    public async Task<ServiceResponse<PromotionCampaignDto>> Handle(
        ClosePromotionCampaignRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.CloseCampaign(
            request.PromotionCampaignId,
            request.ClosedByUserId,
            cancellationToken);
    }
}

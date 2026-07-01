using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record ApprovePromotionCampaignRequest(int PromotionCampaignId, ulong ApprovedByUserId)
    : IRequest<ServiceResponse<PromotionCampaignDto>>;

internal class ApprovePromotionCampaignHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<ApprovePromotionCampaignRequest, ServiceResponse<PromotionCampaignDto>>
{
    public async Task<ServiceResponse<PromotionCampaignDto>> Handle(
        ApprovePromotionCampaignRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.ApproveCampaign(
            request.PromotionCampaignId,
            request.ApprovedByUserId,
            cancellationToken);
    }
}

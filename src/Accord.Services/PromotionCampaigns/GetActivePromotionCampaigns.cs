using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record GetActivePromotionCampaignsRequest : IRequest<IReadOnlyList<ActivePromotionCampaignDto>>;

internal class GetActivePromotionCampaignsHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<GetActivePromotionCampaignsRequest, IReadOnlyList<ActivePromotionCampaignDto>>
{
    public async Task<IReadOnlyList<ActivePromotionCampaignDto>> Handle(
        GetActivePromotionCampaignsRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.GetActiveCampaigns(cancellationToken);
    }
}

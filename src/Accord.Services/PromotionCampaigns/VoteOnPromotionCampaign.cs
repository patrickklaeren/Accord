using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record VoteOnPromotionCampaignRequest(int PromotionCampaignId, ulong VotingUserId, int Vote)
    : IRequest<ServiceResponse<PromotionCampaignDto>>;

internal class VoteOnPromotionCampaignHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<VoteOnPromotionCampaignRequest, ServiceResponse<PromotionCampaignDto>>
{
    public async Task<ServiceResponse<PromotionCampaignDto>> Handle(
        VoteOnPromotionCampaignRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.AddVote(
            request.PromotionCampaignId,
            request.VotingUserId,
            request.Vote,
            cancellationToken);
    }
}

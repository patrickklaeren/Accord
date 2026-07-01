using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.PromotionCampaigns;

public sealed record CreatePromotionCampaignRequest(
    PermissionUser ForUser,
    PermissionUser ByUser,
    PermissionUser VouchedForByUser,
    ulong ToDiscordRoleId) : IRequest<ServiceResponse<PromotionCampaignDto>>;

internal class CreatePromotionCampaignHandler(PromotionCampaignService promotionCampaignService)
    : IRequestHandler<CreatePromotionCampaignRequest, ServiceResponse<PromotionCampaignDto>>
{
    public async Task<ServiceResponse<PromotionCampaignDto>> Handle(
        CreatePromotionCampaignRequest request,
        CancellationToken cancellationToken)
    {
        return await promotionCampaignService.AddCampaign(
            request.ForUser,
            request.ByUser,
            request.VouchedForByUser,
            request.ToDiscordRoleId,
            cancellationToken);
    }
}

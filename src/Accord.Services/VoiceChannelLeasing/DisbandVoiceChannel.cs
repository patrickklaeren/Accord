using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.VoiceChannelLeasing;

public sealed record DisbandVoiceChannelRequest(PermissionUser ForUser) : IRequest<ServiceResponse<VoiceChannelLeaseDto>>;

internal class DisbandVoiceChannelHandler(VoiceChannelLeasingService voiceChannelLeasingService)
    : IRequestHandler<DisbandVoiceChannelRequest, ServiceResponse<VoiceChannelLeaseDto>>
{
    public async Task<ServiceResponse<VoiceChannelLeaseDto>> Handle(
        DisbandVoiceChannelRequest request,
        CancellationToken cancellationToken)
    {
        return await voiceChannelLeasingService.DisbandOwnedChannel(request.ForUser, cancellationToken);
    }
}

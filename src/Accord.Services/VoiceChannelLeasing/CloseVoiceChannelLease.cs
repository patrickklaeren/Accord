using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.VoiceChannelLeasing;

public sealed record CloseVoiceChannelLeaseRequest(
    int LeaseId,
    ulong? ClosedByUserId) : IRequest<ServiceResponse>;

internal class CloseVoiceChannelLeaseHandler(VoiceChannelLeasingService voiceChannelLeasingService)
    : IRequestHandler<CloseVoiceChannelLeaseRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(
        CloseVoiceChannelLeaseRequest request,
        CancellationToken cancellationToken)
    {
        return await voiceChannelLeasingService.CloseLease(
            request.LeaseId,
            request.ClosedByUserId,
            cancellationToken);
    }
}

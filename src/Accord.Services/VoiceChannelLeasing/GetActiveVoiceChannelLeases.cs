using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.VoiceChannelLeasing;

public sealed record GetActiveVoiceChannelLeasesRequest : IRequest<IReadOnlyList<VoiceChannelLeaseDto>>;

internal class GetActiveVoiceChannelLeasesHandler(VoiceChannelLeasingService voiceChannelLeasingService)
    : IRequestHandler<GetActiveVoiceChannelLeasesRequest, IReadOnlyList<VoiceChannelLeaseDto>>
{
    public async Task<IReadOnlyList<VoiceChannelLeaseDto>> Handle(
        GetActiveVoiceChannelLeasesRequest request,
        CancellationToken cancellationToken)
    {
        return await voiceChannelLeasingService.GetActiveLeases(cancellationToken);
    }
}

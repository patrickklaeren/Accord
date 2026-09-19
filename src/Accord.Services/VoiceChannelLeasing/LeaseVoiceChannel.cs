using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.VoiceChannelLeasing;

public sealed record LeaseVoiceChannelRequest(
    PermissionUser ForUser,
    ulong DiscordGuildId,
    string ChannelName,
    int? MaxUsers) : IRequest<ServiceResponse<VoiceChannelLeaseDto>>;

internal class LeaseVoiceChannelHandler(VoiceChannelLeasingService voiceChannelLeasingService)
    : IRequestHandler<LeaseVoiceChannelRequest, ServiceResponse<VoiceChannelLeaseDto>>
{
    public async Task<ServiceResponse<VoiceChannelLeaseDto>> Handle(
        LeaseVoiceChannelRequest request,
        CancellationToken cancellationToken)
    {
        return await voiceChannelLeasingService.LeaseChannel(
            request.ForUser,
            request.DiscordGuildId,
            request.ChannelName,
            request.MaxUsers,
            cancellationToken);
    }
}

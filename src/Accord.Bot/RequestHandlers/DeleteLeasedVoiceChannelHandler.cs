using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.VoiceChannelLeasing;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class DeleteLeasedVoiceChannelHandler(
    IDiscordRestChannelAPI channelApi,
    VoiceChannelLeaseOccupancyTracker occupancyTracker)
    : IRequestHandler<DeleteLeasedVoiceChannelRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(
        DeleteLeasedVoiceChannelRequest request,
        CancellationToken cancellationToken)
    {
        occupancyTracker.StopTracking(request.DiscordChannelId);

        var result = await channelApi.DeleteChannelAsync(
            new Snowflake(request.DiscordChannelId),
            "Leased voice channel released",
            cancellationToken);

        return result.IsSuccess
            ? ServiceResponse.Ok()
            : ServiceResponse.Fail("Failed to delete the voice channel");
    }
}

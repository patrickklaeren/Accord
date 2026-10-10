using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
using Accord.Services.VoiceChannelLeasing;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class CreateLeasedVoiceChannelHandler(
    IDiscordRestGuildAPI guildApi,
    VoiceChannelLeaseOccupancyTracker occupancyTracker,
    IMediator mediator)
    : IRequestHandler<CreateLeasedVoiceChannelRequest, ServiceResponse<CreatedVoiceChannelDto>>
{
    public async Task<ServiceResponse<CreatedVoiceChannelDto>> Handle(
        CreateLeasedVoiceChannelRequest request,
        CancellationToken cancellationToken)
    {
        var leaseCategoryIds = await mediator.Send(
            new GetChannelsWithFlagRequest(ChannelFlagType.VoiceLeaseCategory),
            cancellationToken);

        ulong? parentCategoryId = leaseCategoryIds.Count > 0 ? leaseCategoryIds[0] : null;

        var parentId = parentCategoryId.HasValue
            ? new Optional<Snowflake?>(new Snowflake(parentCategoryId.Value))
            : default;

        var userLimit = request.MaxUsers.HasValue
            ? new Optional<int?>(request.MaxUsers.Value)
            : default;

        var result = await guildApi.CreateGuildVoiceChannelAsync(
            new Snowflake(request.DiscordGuildId),
            request.ChannelName,
            userLimit: userLimit,
            parentID: parentId,
            reason: "Leased voice channel",
            ct: cancellationToken);

        if (!result.IsSuccess)
        {
            return ServiceResponse.Fail<CreatedVoiceChannelDto>("Failed to create the voice channel");
        }

        occupancyTracker.StartTracking(result.Entity.ID.Value);

        return ServiceResponse.Ok(new CreatedVoiceChannelDto(result.Entity.ID.Value, parentCategoryId));
    }
}

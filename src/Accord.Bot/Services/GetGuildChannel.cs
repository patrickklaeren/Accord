using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.Services;

public class GetGuildChannelHandler(IDiscordRestChannelAPI channelApi) 
    : IRequestHandler<GetDiscordGuildChannelRequest, ServiceResponse<DiscordGuildChannelDto>>
{
    public async Task<ServiceResponse<DiscordGuildChannelDto>> Handle(GetDiscordGuildChannelRequest request, CancellationToken cancellationToken)
    {
        var channel = await channelApi.GetChannelAsync(new Snowflake(request.DiscordChannelId), cancellationToken);

        if (!channel.IsSuccess)
        {
            return ServiceResponse.Fail<DiscordGuildChannelDto>(channel.Error.Message);
        }

        var dto = new DiscordGuildChannelDto(channel.Entity.ID.Value,
            channel.Entity.Name.HasValue ? channel.Entity.Name.Value : null,
            channel.Entity.ParentID.HasValue ? channel.Entity.ParentID.Value?.Value : null,
            channel.Entity.OwnerID.HasValue ? channel.Entity.OwnerID.Value.Value : null);

        return ServiceResponse.Ok(dto);
    }
}
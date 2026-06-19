using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class DeleteDiscordMessageHandler(IDiscordRestChannelAPI channelApi) : IRequestHandler<DeleteDiscordMessageRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(DeleteDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        var response = await channelApi
            .DeleteMessageAsync(new Snowflake(request.DiscordChannelId), new Snowflake(request.DiscordMessageId), ct: cancellationToken);
        
        return response.IsSuccess ? ServiceResponse.Ok() : ServiceResponse.Fail(response.Error.Message);
    }
}
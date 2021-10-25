using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers;

public class DeleteUserReportDiscordMessageHandler : IRequestHandler<DeleteUserReportDiscordMessageRequest, ServiceResponse>
{
    private readonly IDiscordRestWebhookAPI _webhookApi;

    public DeleteUserReportDiscordMessageHandler(IDiscordRestWebhookAPI webhookApi)
    {
        _webhookApi = webhookApi;
    }
        
    public async Task<ServiceResponse> Handle(DeleteUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        await _webhookApi.DeleteWebhookMessageAsync(
            new Snowflake(request.DiscordProxyWebhookId),
            request.DiscordProxyWebhookToken,
            new Snowflake(request.DiscordProxiedMessageId),
            ct: cancellationToken);
        return ServiceResponse.Ok();
    }
}
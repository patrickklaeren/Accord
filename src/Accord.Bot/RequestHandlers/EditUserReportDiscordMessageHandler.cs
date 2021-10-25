using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers;

public class EditUserReportDiscordMessageHandler : IRequestHandler<EditUserReportDiscordMessageRequest, ServiceResponse>
{
    private readonly IDiscordRestWebhookAPI _webhookApi;

    public EditUserReportDiscordMessageHandler(IDiscordRestWebhookAPI webhookApi)
    {
        _webhookApi = webhookApi;
    }

    public async Task<ServiceResponse> Handle(EditUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        await _webhookApi.EditWebhookMessageAsync(
            new Snowflake(request.DiscordProxyWebhookId),
            request.DiscordProxyWebhookToken,
            new Snowflake(request.DiscordProxiedMessageId),
            request.Content, 
            ct: cancellationToken);
        return ServiceResponse.Ok();
    }
}
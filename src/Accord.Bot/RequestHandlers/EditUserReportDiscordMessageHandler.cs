using System.Threading;
using System.Threading.Tasks;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class EditUserReportDiscordMessageHandler(IDiscordRestWebhookAPI webhookApi) : IRequestHandler<EditUserReportDiscordMessageRequest>
{

    public async Task Handle(EditUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        await webhookApi.EditWebhookMessageAsync(
            new Snowflake(request.DiscordProxyWebhookId),
            request.DiscordProxyWebhookToken,
            new Snowflake(request.DiscordProxiedMessageId),
            request.Content,
            ct: cancellationToken);
    }
}
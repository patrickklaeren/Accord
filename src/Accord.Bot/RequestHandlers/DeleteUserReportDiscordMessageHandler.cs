using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class DeleteUserReportDiscordMessageHandler(IDiscordRestWebhookAPI webhookApi) : IRequestHandler<DeleteUserReportDiscordMessageRequest>
{

    public async Task Handle(DeleteUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        await webhookApi.DeleteWebhookMessageAsync(
            new Snowflake(request.DiscordProxyWebhookId),
            request.DiscordProxyWebhookToken,
            new Snowflake(request.DiscordProxiedMessageId),
            ct: cancellationToken);
    }
}
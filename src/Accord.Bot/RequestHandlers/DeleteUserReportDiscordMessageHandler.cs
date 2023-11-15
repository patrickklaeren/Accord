using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

[AutoConstructor]
public partial class DeleteUserReportDiscordMessageHandler : IRequestHandler<DeleteUserReportDiscordMessageRequest>
{
    private readonly IDiscordRestWebhookAPI _webhookApi;

    public async Task Handle(DeleteUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        await _webhookApi.DeleteWebhookMessageAsync(
            new Snowflake(request.DiscordProxyWebhookId),
            request.DiscordProxyWebhookToken,
            new Snowflake(request.DiscordProxiedMessageId),
            ct: cancellationToken);
    }
}
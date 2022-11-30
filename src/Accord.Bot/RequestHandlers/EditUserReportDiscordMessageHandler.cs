using System.Threading;
using System.Threading.Tasks;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

[AutoConstructor]
public partial class EditUserReportDiscordMessageHandler : AsyncRequestHandler<EditUserReportDiscordMessageRequest>
{
    private readonly IDiscordRestWebhookAPI _webhookApi;

    protected override async Task Handle(EditUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        await _webhookApi.EditWebhookMessageAsync(
            new Snowflake(request.DiscordProxyWebhookId),
            request.DiscordProxyWebhookToken,
            new Snowflake(request.DiscordProxiedMessageId),
            request.Content, 
            ct: cancellationToken);
    }
}
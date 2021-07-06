using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Discord.Rest;
using Remora.Results;

namespace Accord.Bot.RequestHandlers
{
    public class DeleteUserReportDiscordMessageHandler : IRequestHandler<DeleteUserReportDiscordMessageRequest, ServiceResponse>
    {
        private readonly IDiscordRestWebhookAPI _webhookApi;
        //todo remove
        private readonly DiscordHttpClient _discordHttpClient;

        public DeleteUserReportDiscordMessageHandler(IDiscordRestWebhookAPI webhookApi, DiscordHttpClient discordHttpClient)
        {
            _webhookApi = webhookApi;
            _discordHttpClient = discordHttpClient;
        }

        //todo remove when https://github.com/Nihlus/Remora.Discord/pull/73 gets merged.
        private async Task<Result> DeleteWebhookMessageAsync(
            Snowflake webhookID,
            string token,
            Snowflake messageID,
            CancellationToken ct = default
        ) => await _discordHttpClient.DeleteAsync($"webhooks/{webhookID}/{token}/messages/{messageID}", ct: ct);
        
        public async Task<ServiceResponse> Handle(DeleteUserReportDiscordMessageRequest request, CancellationToken cancellationToken)
        {
            //todo change to _webhookApi.DeleteWebhookMessageAsync
            await DeleteWebhookMessageAsync(
                new Snowflake(request.DiscordProxyWebhookId),
                request.DiscordProxyWebhookToken,
                new Snowflake(request.DiscordProxiedMessageId),
                ct: cancellationToken);
            return ServiceResponse.Ok();
        }
    }
}
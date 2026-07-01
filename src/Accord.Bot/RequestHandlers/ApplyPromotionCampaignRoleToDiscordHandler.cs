using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.PromotionCampaigns;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class ApplyPromotionCampaignRoleToDiscordHandler(
    IDiscordRestGuildAPI guildApi,
    DiscordConfiguration discordConfiguration) : IRequestHandler<ApplyPromotionCampaignRoleToDiscordRequest, bool>
{
    public async Task<bool> Handle(
        ApplyPromotionCampaignRoleToDiscordRequest request,
        CancellationToken cancellationToken)
    {
        var response = await guildApi.AddGuildMemberRoleAsync(
            new Snowflake(discordConfiguration.GuildId),
            new Snowflake(request.DiscordUserId),
            new Snowflake(request.DiscordRoleId),
            $"Campaign #{request.PromotionCampaignId} approved by {request.ApprovedByUserId}",
            cancellationToken);

        return response.IsSuccess;
    }
}
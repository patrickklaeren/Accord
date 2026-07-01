using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.RequestHandlers;
using Accord.Services.PromotionCampaigns;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class PromotionCampaignVoteResponder(
    IMediator mediator,
    IDiscordRestInteractionAPI interactionApi,
    PromotionCampaignMessageFactory promotionCampaignMessageFactory) : IResponder<IInteractionCreate>
{
    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct)
    {
        if (gatewayEvent.Type is not InteractionType.MessageComponent)
        {
            return Result.FromSuccess();
        }

        if (!gatewayEvent.Data.HasValue || !gatewayEvent.Data.Value.IsT1)
        {
            return Result.FromSuccess();
        }

        var componentData = gatewayEvent.Data.Value.AsT1;
        if (!componentData.CustomID.StartsWith(PromotionCampaignMessageFactory.VOTE_CUSTOM_ID_PREFIX))
        {
            return Result.FromSuccess();
        }

        if (!gatewayEvent.Member.HasValue || !gatewayEvent.Member.Value.User.HasValue)
        {
            return Result.FromSuccess();
        }

        var idValue = componentData.CustomID.Replace(PromotionCampaignMessageFactory.VOTE_CUSTOM_ID_PREFIX, string.Empty);
        if (!int.TryParse(idValue, out var campaignId))
        {
            return Result.FromSuccess();
        }

        var votingUserId = gatewayEvent.Member.Value.User.Value.ID.Value;
        var response = await mediator.Send(new VoteOnPromotionCampaignRequest(campaignId, votingUserId), ct);

        if (!response.Success)
        {
            var callback = new InteractionMessageCallbackData(
                Content: response.ErrorMessage,
                Flags: MessageFlags.Ephemeral,
                AllowedMentions: new AllowedMentions(Parse: new List<MentionType>()));

            return await interactionApi.CreateInteractionResponseAsync(
                gatewayEvent.ID,
                gatewayEvent.Token,
                new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, new(callback)),
                ct: ct);
        }

        var campaign = response.Value!;
        var embed = promotionCampaignMessageFactory.CreateEmbed(gatewayEvent.User.Value, campaign);
        var components = promotionCampaignMessageFactory.CreateComponents(campaign.Id);
        
        var update = new InteractionMessageCallbackData(
            Embeds: new[] { embed },
            Components: components.ToList(),
            AllowedMentions: new AllowedMentions(Parse: new List<MentionType>()));

        return await interactionApi.CreateInteractionResponseAsync(
            gatewayEvent.ID,
            gatewayEvent.Token,
            new InteractionResponse(InteractionCallbackType.UpdateMessage, new(update)),
            ct: ct);
    }
}

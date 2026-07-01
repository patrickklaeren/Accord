using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.PromotionCampaigns;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Responders;

public class PromotionCampaignVoteResponder(
    IMediator mediator,
    IDiscordRestInteractionAPI interactionApi,
    IDiscordRestUserAPI userApi,
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

        var voteCommand = componentData.CustomID.Replace(PromotionCampaignMessageFactory.VOTE_CUSTOM_ID_PREFIX, string.Empty);
        var split = voteCommand.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        var vote = 1;
        int campaignId;
        
        // Legacy switchover can be removed once everything is migrated
        if (split.Length is 2)
        {
            if (!int.TryParse(split[0], out vote))
            {
                return Result.FromSuccess();   
            }
        
            if (!int.TryParse(split[1], out campaignId))
            {
                return Result.FromSuccess();
            }
        }
        else
        {
            if (!int.TryParse(split[0], out campaignId))
            {
                return Result.FromSuccess();
            }
        }

        var votingUserId = gatewayEvent.Member.Value.User.Value.ID.Value;
        var response = await mediator.Send(new VoteOnPromotionCampaignRequest(campaignId, votingUserId, vote), ct);

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
        var user = await userApi.GetUserAsync(new Snowflake(campaign.ForUserId), ct);
        var embed = promotionCampaignMessageFactory.CreateEmbed(user.Entity, campaign);
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.PromotionCampaigns;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RelayPromotionCampaignToDiscordHandler(IDiscordRestChannelAPI channelApi, IDiscordRestUserAPI userApi, PromotionCampaignMessageFactory promotionCampaignMessageFactory) 
    : IRequestHandler<RelayNewPromotionCampaignToDiscordRequest, ulong?>,
        IRequestHandler<RelayExistingPromotionCampaignToDiscordRequest>
{
    public async Task<ulong?> Handle(RelayNewPromotionCampaignToDiscordRequest request, CancellationToken cancellationToken)
    {
        var user = await userApi.GetUserAsync(new Snowflake(request.Campaign.ForUserId), cancellationToken);

        if (!user.IsSuccess)
            return null;
        
        var embed = promotionCampaignMessageFactory.CreateEmbed(user.Entity, request.Campaign);
        var components = promotionCampaignMessageFactory.CreateComponents(request.Campaign.Id);

        var messageResponse = await channelApi.CreateMessageAsync(
            new Snowflake(request.DiscordChannelId),
            embeds: new[] { embed },
            components: components.ToList(),
            allowedMentions: new AllowedMentions(Parse: new List<MentionType>()),
            ct: cancellationToken);

        return messageResponse.IsSuccess ? messageResponse.Entity.ID.Value : null;
    }

    public async Task Handle(RelayExistingPromotionCampaignToDiscordRequest request, CancellationToken cancellationToken)
    {
        var user = await userApi.GetUserAsync(new Snowflake(request.Campaign.ForUserId), cancellationToken);

        if (!user.IsSuccess)
            return;
        
        var embed = promotionCampaignMessageFactory.CreateEmbed(user.Entity, request.Campaign);

        IReadOnlyCollection<IMessageComponent> components = [];

        if (request.Campaign.EndDateTime > DateTimeOffset.Now)
        {
            components = promotionCampaignMessageFactory.CreateComponents(request.Campaign.Id);   
        }

        await channelApi.EditMessageAsync(
            new Snowflake(request.DiscordChannelId),
            new Snowflake(request.DiscordMessageId),
            embeds: new[] { embed },
            components: components.ToList(),
            allowedMentions: new AllowedMentions(Parse: new List<MentionType>()),
            ct: cancellationToken);
    }
}

[RegisterScoped]
public class PromotionCampaignMessageFactory(ThumbnailHelper thumbnailHelper)
{
    public const string VOTE_CUSTOM_ID_PREFIX = "promotion-campaign-vote:";

    public Embed CreateEmbed(IUser user, PromotionCampaignDto campaign)
    {
        var avatar = thumbnailHelper.GetAvatar(user);
        var formattedTime = DiscordFormatter.TimeToMarkdown(campaign.EndDateTime);
        
        return new Embed(
            Title: $"{user.Username} is campaigning",
            Thumbnail: avatar,
            Description: $"{DiscordFormatter.UserIdToMention(campaign.ForUserId)} is campaigning for <@&{campaign.ToDiscordRoleId}> until {formattedTime}. Support their campaign by pressing 👍!",
            Colour: Color.DodgerBlue,
            Fields: new[]
            {
                new EmbedField("Required votes", campaign.VoteThresholdRequired.ToString(), true),
                new EmbedField("Obtained votes", $"{campaign.VoteProgress}", true),
            },
            Footer: new EmbedFooter($"Campaign #{campaign.Id} - Achieving the obtained votes does not guarantee promotion!"));
    }

    public IReadOnlyCollection<IMessageComponent> CreateComponents(int campaignId)
    {
        return
        [
            new ActionRowComponent([
                new ButtonComponent(
                    ButtonComponentStyle.Success,
                    "👍",
                    default,
                    $"{VOTE_CUSTOM_ID_PREFIX}{campaignId}")
            ])
        ];
    }
}
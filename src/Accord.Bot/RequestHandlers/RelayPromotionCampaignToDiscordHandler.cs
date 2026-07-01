using System;
using System.Collections.Generic;
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
        IRequestHandler<RelayExistingPromotionCampaignToDiscordRequest>,
        IRequestHandler<RelayApprovedPromotionCampaignToDiscordRequest>
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

        if (request.Campaign.EndDateTime > DateTimeOffset.UtcNow && request.Campaign.ClosedDateTime is null)
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

    public async Task Handle(RelayApprovedPromotionCampaignToDiscordRequest request, CancellationToken cancellationToken)
    {
        await channelApi.CreateMessageAsync(
            new Snowflake(request.DiscordChannelId),
            $"{DiscordFormatter.UserIdToMention(request.Campaign.ForUserId)} your campaign was successful! You now have the {DiscordFormatter.RoleIdToMention(request.Campaign.ToDiscordRoleId)} role!",
            allowedMentions: new AllowedMentions(Parse: new[] { MentionType.Users }),
            ct: cancellationToken);
    }
}
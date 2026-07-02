using System.Collections.Generic;
using System.Drawing;
using Accord.Services.PromotionCampaigns;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class PromotionCampaignMessageFactory(ThumbnailHelper thumbnailHelper)
{
    public const string VOTE_CUSTOM_ID_PREFIX = "promotion-campaign-vote:";
    public const string VOTE_INFO_CUSTOM_ID_PREFIX = "promotion-campaign-info:";

    public Embed CreateEmbed(IUser user, PromotionCampaignDto campaign)
    {
        var avatar = thumbnailHelper.GetAvatar(user);
        var formattedTime = DiscordFormatter.TimeToMarkdown(campaign.EndDateTime);
        var userMention = DiscordFormatter.UserIdToMention(campaign.ForUserId);
        var roleMention = DiscordFormatter.RoleIdToMention(campaign.ToDiscordRoleId);

        var title = $"{user.Username} is campaigning";
        var description = $"{userMention} is campaigning for {roleMention} until {formattedTime}.";
        
        var colour = campaign.ClosedDateTime is not null
            ? campaign.IsApproved
                ? Color.Green
                : Color.DimGray
            : Color.DodgerBlue;

        if (campaign.ClosedDateTime is not null)
        {
            title = $"{user.Username} campaigned";
            description = $"{userMention} campaigned for {roleMention}";

            if (campaign.IsApproved)
            {
                var closedFormattedTime = DiscordFormatter.TimeToMarkdown(campaign.ClosedDateTime.Value);
                description += $" and was successful on {closedFormattedTime} 🎉";
                colour = Color.Green;
            }
        }
        
        return new Embed(
            Title: title,
            Thumbnail: avatar,
            Description: description,
            Colour: colour,
            Fields: new[]
            {
                new EmbedField("Required votes", campaign.VoteThresholdRequired.ToString(), true),
                new EmbedField("Total votes", $"{campaign.TotalVotes}", true),
            },
            Footer: new EmbedFooter($"#{campaign.Id} - Achieving required votes does not guarantee promotion!"));
    }

    public IReadOnlyCollection<IMessageComponent> CreateComponents(int campaignId, ulong campaignUserId)
    {
        return
        [
            new ActionRowComponent([
                new ButtonComponent(
                    ButtonComponentStyle.Success,
                    "+1",
                    Emoji: new PartialEmoji(Name: "👍"),
                    $"{VOTE_CUSTOM_ID_PREFIX}1:{campaignId}"),

                new ButtonComponent(
                    ButtonComponentStyle.Danger,
                    "-1",
                    Emoji: new PartialEmoji(Name: "👎"),
                    $"{VOTE_CUSTOM_ID_PREFIX}-1:{campaignId}"),

                new ButtonComponent(
                    ButtonComponentStyle.Secondary,
                    "Abstain",
                    Emoji: new PartialEmoji(Name: "🤷"),
                    $"{VOTE_CUSTOM_ID_PREFIX}0:{campaignId}"),

                new ButtonComponent(
                    ButtonComponentStyle.Secondary,
                    "Profile",
                    Emoji: new PartialEmoji(Name: "👀"),
                    $"{VOTE_INFO_CUSTOM_ID_PREFIX}{campaignUserId}")
            ])
        ];
    }
}

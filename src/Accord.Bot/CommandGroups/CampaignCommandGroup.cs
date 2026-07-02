using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.PromotionCampaigns;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("campaign")]
public class CampaignCommandGroup(
    ICommandContext commandContext,
    IMediator mediator,
    PermissionUserFactory permissionUserFactory,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("myself"), Description("Open a promotion campaign for yourself"), Ephemeral]
    public async Task<IResult> CampaignMyself(IRole role, IGuildMember vouchedForByUser)
    {
        if (!vouchedForByUser.User.HasValue)
        {
            return await feedbackService.SendContextualAsync("Vouching user not found");
        }

        var executingUser = await commandContext.ToPermissionUser(permissionUserFactory);
        var vouchingUser = await permissionUserFactory.FromId(vouchedForByUser.User.Value.ID.Value);

        var response = await mediator.Send(new CreatePromotionCampaignRequest(
            executingUser,
            executingUser,
            vouchingUser,
            role.ID.Value));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync("Campaign started!"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("for"), Description("Open a promotion campaign for another user"), Ephemeral]
    public async Task<IResult> CampaignFor(IGuildMember user, IRole role)
    {
        if (!user.User.HasValue)
        {
            return await feedbackService.SendContextualAsync("Campaign user not found");
        }
        
        var targetUser = await permissionUserFactory.FromId(user.User.Value.ID.Value);
        var vouchingUser = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new CreatePromotionCampaignRequest(
            targetUser,
            vouchingUser,
            vouchingUser,
            role.ID.Value));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync("Campaign started!"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("close"), Description("Close a promotion campaign without promoting the user"), Ephemeral]
    public async Task<IResult> Close(int campaignId)
    {
        var user = commandContext.GetExecutingUser();
        var response = await mediator.Send(new ClosePromotionCampaignRequest(campaignId, user.ID.Value));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Campaign #{campaignId} closed!"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("approve"), Description("Approve a promotion campaign and apply the role"), Ephemeral]
    public async Task<IResult> Approve(int campaignId)
    {
        var user = commandContext.GetExecutingUser();
        var response = await mediator.Send(new ApprovePromotionCampaignRequest(campaignId, user.ID.Value));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Campaign #{campaignId} approved"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("list"), Description("List active promotion campaigns"), Ephemeral]
    public async Task<IResult> List()
    {
        var campaigns = await mediator.Send(new GetActivePromotionCampaignsRequest());

        if (!campaigns.Any())
        {
            return await feedbackService.SendContextualEmbedAsync(new Embed(
                Title: "Active Promotion Campaigns",
                Description: "No active campaigns."));
        }

        var description = new StringBuilder();

        foreach (var campaign in campaigns)
        {
            description.AppendLine($"**#{campaign.Id}** {DiscordFormatter.UserIdToMention(campaign.ForUserId)} for {DiscordFormatter.RoleIdToMention(campaign.ToDiscordRoleId)}");
            description.AppendLine($"Started by {DiscordFormatter.UserIdToMention(campaign.ByUserId)} · vouched by {DiscordFormatter.UserIdToMention(campaign.VouchedForByUserId)} · closes {DiscordFormatter.TimeToMarkdown(campaign.EndDateTime, TimeToMentionType.RelativeTime)}");
            description.AppendLine();
        }

        var embed = new Embed(
            Title: "Active Promotion Campaigns",
            Description: description.ToString());

        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("details"), Description("Show promotion campaign details and votes"), Ephemeral]
    public async Task<IResult> Details(int campaignId)
    {
        var campaign = await mediator.Send(new GetPromotionCampaignDetailsRequest(campaignId));

        if (campaign is null)
        {
            return await feedbackService.SendContextualAsync($"Campaign #{campaignId} not found.");
        }

        var description = new StringBuilder()
            .AppendLine($"Campaign for {DiscordFormatter.UserIdToMention(campaign.ForUserId)} to receive {DiscordFormatter.RoleIdToMention(campaign.ToDiscordRoleId)}")
            .AppendLine($"Created by {DiscordFormatter.UserIdToMention(campaign.ByUserId)}")
            .AppendLine($"Vouched by {DiscordFormatter.UserIdToMention(campaign.VouchedForByUserId)}")
            .AppendLine($"Started {DiscordFormatter.TimeToMarkdown(campaign.StartDateTime)}")
            .AppendLine($"Ends {DiscordFormatter.TimeToMarkdown(campaign.EndDateTime)}")
            .AppendLine($"Required vote score: {campaign.VoteThresholdRequired}")
            .AppendLine($"Current vote score: {campaign.TotalVoteScore}");

        if (campaign.ClosedDateTime is not null)
        {
            description.AppendLine($"Closed {DiscordFormatter.TimeToMarkdown(campaign.ClosedDateTime.Value)}");
        }

        if (campaign.ClosedByUserId is not null)
        {
            description.AppendLine($"Closed by {DiscordFormatter.UserIdToMention(campaign.ClosedByUserId.Value)}");
        }

        description.AppendLine($"Status: {GetCampaignStatus(campaign)}");

        var embed = new Embed(
            Title: $"Promotion Campaign #{campaign.Id}",
            Description: description.ToString(),
            Fields: new[]
            {
                CreateVoteField("For (+1)", campaign.Votes.Where(x => x.Vote > 0)),
                CreateVoteField("Against (-1)", campaign.Votes.Where(x => x.Vote < 0)),
                CreateVoteField("Abstaining (0)", campaign.Votes.Where(x => x.Vote == 0))
            });

        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    private static string GetCampaignStatus(PromotionCampaignDetailsDto campaign)
    {
        if (campaign.ClosedDateTime is null)
        {
            return campaign.EndDateTime > System.DateTimeOffset.UtcNow ? "Open" : "Expired";
        }

        return campaign.IsApproved ? "Approved" : "Closed";
    }

    private static EmbedField CreateVoteField(string name, IEnumerable<PromotionCampaignVoteDetailsDto> votes)
    {
        var voteList = votes.ToList();
        var value = voteList.Any()
            ? string.Join("\n", voteList.Select(x => $"{DiscordFormatter.UserIdToMention(x.VotingUserId)} · {DiscordFormatter.TimeToMarkdown(x.AtDateTime, TimeToMentionType.RelativeTime)}"))
            : "No votes";

        return new EmbedField($"{name} ({voteList.Count})", value, false);
    }
}

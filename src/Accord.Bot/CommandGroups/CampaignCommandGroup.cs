using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.PromotionCampaigns;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
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
}

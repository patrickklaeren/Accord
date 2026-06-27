using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.HelpForum;
using Humanizer;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class HelpForumCommandGroup(
    ICommandContext commandContext,
    FeedbackService feedbackService,
    IDiscordRestChannelAPI channelApi,
    PermissionUserFactory permissionUserFactory,
    IMediator mediator) : AccordCommandGroup
{
    [Command("solve"), Description("Mark the current channel as solved (usually for help forums)"), Ephemeral]
    public async Task<IResult> Solve()
    {
        var context = commandContext.GetCommandProxy();
        var user = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new CanCloseHelpForumPostRequest(user, context.ChannelId.Value));

        if (!response.Success)
        {
            return await feedbackService.SendContextualAsync(response.ErrorMessage);
        }

        if (await HandleSolve(context.ChannelId))
        {
            var lockResponse = await channelApi.ModifyThreadChannelAsync(context.ChannelId,
                isArchived: true,
                isLocked: true);

            if (!lockResponse.IsSuccess)
            {
                return await feedbackService.SendContextualAsync($"Failed closing post - {lockResponse.Error.Message}");
            }
        }

        return await feedbackService.SendContextualAsync("Solved!");
    }

    [Command("close"), Description("Mark the channel as closed for a reason (usually for help forums)"), Ephemeral]
    public async Task<IResult> Close(CloseReason reason)
    {
        var context = commandContext.GetCommandProxy();
        var user = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new CanCloseHelpForumPostRequest(user, context.ChannelId.Value));

        if (!response.Success)
        {
            return await feedbackService.SendContextualAsync(response.ErrorMessage);
        }

        bool lockPost;
        
        if (reason is CloseReason.Solved)
        {
            lockPost = await HandleSolve(context.ChannelId);
        }
        else
        {
            lockPost = await HandleClose(context.ChannelId, reason);
        }

        if (lockPost)
        {
            var lockResponse = await channelApi.ModifyThreadChannelAsync(context.ChannelId,
                isArchived: true,
                isLocked: true);

            if (!lockResponse.IsSuccess)
            {
                return await feedbackService.SendContextualAsync($"Failed closing post - {lockResponse.Error.Message}");
            }
        }
        
        return await feedbackService.SendContextualAsync("Closed!");
    }

    private async Task<bool> HandleSolve(Snowflake channelId)
    {
        var topMessage = await channelApi.GetChannelMessagesAsync(channelId, around: channelId, limit: 1);

        if (topMessage is { IsSuccess: true, Entity: [{ } messageToAction, ..] })
        {
            await channelApi.DeleteAllReactionsAsync(channelId, messageToAction.ID);
            await channelApi.CreateReactionAsync(channelId, messageToAction.ID, "✅");
        }

        var message = await channelApi.CreateMessageAsync(channelId, $"✅ Marked as solved!");
        return message.IsSuccess;
    }

    private async Task<bool> HandleClose(Snowflake channelId, CloseReason reason)
    {
        var topMessage = await channelApi.GetChannelMessagesAsync(channelId, around: channelId, limit: 1);

        if (topMessage is { IsSuccess: true, Entity: [{ } messageToAction, ..] })
        {
            await channelApi.DeleteAllReactionsAsync(channelId, messageToAction.ID);
            await channelApi.CreateReactionAsync(channelId, messageToAction.ID, "🔒");
        }

        var message = await channelApi.CreateMessageAsync(channelId, $"Closed for being {reason}");
        return message.IsSuccess;
    }

    public enum CloseReason
    {
        Solved,
        Stale,
        Duplicate,
        Offtopic
    }
}
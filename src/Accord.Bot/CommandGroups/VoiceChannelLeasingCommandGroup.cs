using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.VoiceChannelLeasing;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("voice")]
public class VoiceChannelLeasingCommandGroup(
    ICommandContext commandContext,
    FeedbackService feedbackService,
    PermissionUserFactory permissionUserFactory,
    IMediator mediator)
    : AccordCommandGroup
{
    [Command("lease"), Description("Lease a voice channel that is automatically removed once everyone leaves"), Ephemeral]
    public async Task<IResult> LeaseVoiceChannel(
        [MinValue(VoiceChannelLeasingService.MIN_USER_LIMIT)]
        [MaxValue(VoiceChannelLeasingService.MAX_USER_LIMIT)]
        int? maxUsers = null)
    {
        var proxy = commandContext.GetCommandProxy();
        var executingUser = await commandContext.ToPermissionUser(permissionUserFactory);

        var channelName = $"{commandContext.GetExecutingUser().Username}'s channel";

        var response = await mediator.Send(new LeaseVoiceChannelRequest(
            executingUser,
            proxy.GuildId.Value,
            channelName,
            maxUsers));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync(
                $"Voice channel **{response.Value!.ChannelName}** created. It will be removed automatically once everyone leaves."),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("disband"), Description("Disbands your leased voice channel"), Ephemeral]
    public async Task<IResult> DisbandVoiceChannel()
    {
        var executingUser = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new DisbandVoiceChannelRequest(executingUser));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync(
                $"Voice channel **{response.Value!.ChannelName}** has been disbanded."),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }
}

using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ModMailCommandGroup(ICommandContext commandContext, FeedbackService feedbackService,
    IMediator mediator, IDiscordRestChannelAPI channelApi, DiscordConfiguration configuration,
    JumpLinkHelper jumpLinks, ILogger<ModMailCommandGroup> logger) : AccordCommandGroup
{
    [Command("Report to ModMail"), CommandType(ApplicationCommandType.Message), Ephemeral]
    public async Task<IResult> Report(ulong message)
    {
        if (commandContext is not IInteractionCommandContext)
            return Result.FromSuccess();

        var context = commandContext.GetCommandProxy();
        if (context.GuildId.Value != configuration.GuildId)
            return await feedbackService.SendContextualAsync("ModMail is unavailable in this server.", ct: CancellationToken);

        var channels = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.ModMail), CancellationToken);
        if (channels is not [var channelId])
        {
            var setup = channels.Count == 0
                ? "No ModMail channel is set. Ask a moderator to run `/channel-flag add type:ModMail` in the private channel Ticket Tool monitors."
                : "Several channels have the ModMail flag. Ask a moderator to run `/channel-flag remove type:ModMail` in each extra channel, keeping the private channel Ticket Tool monitors.";
            return await feedbackService.SendContextualAsync(setup, ct: CancellationToken);
        }

        var messageLink = jumpLinks.FromIds(context.ChannelId.Value, message);
        var command = $"{configuration.TicketToolPrefix}new {context.UserId.Value} Reported message: {messageLink}";
        var sent = await channelApi.CreateMessageAsync(new Snowflake(channelId), command, ct: CancellationToken);
        if (!sent.IsSuccess)
        {
            logger.LogWarning("Failed to send a ModMail request to channel {ChannelId}: {Error}", channelId, sent.Error.Message);
            return await feedbackService.SendContextualAsync("Accord couldn't send your report to ModMail.", ct: CancellationToken);
        }

        return await feedbackService.SendContextualAsync("Your report was sent to Ticket Tool.", ct: CancellationToken);
    }
}

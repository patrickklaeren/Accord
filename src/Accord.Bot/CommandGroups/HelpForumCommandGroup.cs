using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Accord.Bot.RequestHandlers;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class HelpForumCommandGroup(ICommandContext commandContext, IDiscordRestGuildAPI guildApi, FeedbackService feedbackService, IDiscordRestChannelAPI channelApi, IMediator mediator) : AccordCommandGroup
{

    private static readonly Snowflake[] AllowedRolesToCloseForumPosts =
    {
        new(268470383571632128), // staff
        new(410138154516086794), // associate
        new(1007745504395939963) // forum helper
    };

    [Command("close"), Description("Mark the current forum thread as answered"), Ephemeral]
    public async Task<IResult> Close()
    {
        var proxy = commandContext.GetCommandProxy();

        var currentChannel = await channelApi.GetChannelAsync(proxy.ChannelId);

        if (!currentChannel.IsSuccess || !currentChannel.Entity.ThreadMetadata.HasValue)
            return Result.FromSuccess();

        if (currentChannel.Entity.OwnerID != proxy.UserId)
        {
            var member = await guildApi.GetGuildMemberAsync(proxy.GuildId, proxy.UserId);

            if (!member.IsSuccess
                || !member.Entity.Roles.Any(d => AllowedRolesToCloseForumPosts.Contains(d)))
            {
                return await feedbackService.SendContextualAsync("Ask the thread owner or member with permission to close this!");
            }
        }

        await mediator.Send(new CloseHelpForumPostRequest(currentChannel.Entity));

        return await feedbackService.SendContextualAsync("Closed!");
    }
}
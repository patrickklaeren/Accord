using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.RequestHandlers;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using Serilog;

namespace Accord.Bot.CommandGroups;

public class HelpForumCommandGroup : AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly CommandResponder _commandResponder;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IMediator _mediator;

    public HelpForumCommandGroup(ICommandContext commandContext,
        IDiscordRestGuildAPI guildApi,
        CommandResponder commandResponder,
        IDiscordRestChannelAPI channelApi,
        IMediator mediator)
    {
        _commandContext = commandContext;
        _guildApi = guildApi;
        _commandResponder = commandResponder;
        _channelApi = channelApi;
        _mediator = mediator;
    }

    private static readonly Snowflake[] AllowedRolesToCloseForumPosts = 
    {
        new(268470383571632128), // staff
        new(410138154516086794), // associate
        new(1007745504395939963) // forum helper
    };

    [Command("close"), Description("Mark the current forum thread as answered")]
    public async Task<IResult> Close()
    {
        if (_commandContext.User.IsBot == true)
            return Result.FromSuccess();

        var currentChannel = await _channelApi.GetChannelAsync(_commandContext.ChannelID);
        
        if (!currentChannel.IsSuccess || !currentChannel.Entity.ThreadMetadata.HasValue)
            return Result.FromSuccess();

        if (currentChannel.Entity.OwnerID != _commandContext.User.ID)
        {
            var member = await _guildApi.GetGuildMemberAsync(_commandContext.GuildID.Value, _commandContext.User.ID);

            if (!member.IsSuccess 
                || !member.Entity.Roles.Any(d => AllowedRolesToCloseForumPosts.Contains(d)))
            {
                await _commandResponder.Respond("Ask the thread owner or member with permission to close this!");
                return Result.FromSuccess();
            }
        }

        await _mediator.Send(new CloseHelpForumPostRequest(currentChannel.Entity));

        return Result.FromSuccess();
    }
}
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using Serilog;

namespace Accord.Bot.CommandGroups;

public class QuestionThreadCommandGroup : AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly CommandResponder _commandResponder;
    private readonly IDiscordRestChannelAPI _channelApi;

    public QuestionThreadCommandGroup(ICommandContext commandContext,
        IDiscordRestGuildAPI guildApi,
        CommandResponder commandResponder,
        IDiscordRestChannelAPI channelApi)
    {
        _commandContext = commandContext;
        _guildApi = guildApi;
        _commandResponder = commandResponder;
        _channelApi = channelApi;
    }

    private const string ANSWERED_POST_SUFFIX = " [Answered]";

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

        if (currentChannel.Entity.Name.HasValue 
            && !currentChannel.Entity.Name.Value!.EndsWith(ANSWERED_POST_SUFFIX))
        {
            try
            {
                var newTitle = currentChannel.Entity.Name.Value!.TrimEnd() + " [Answered]";
                await _channelApi.ModifyThreadChannelAsync(_commandContext.ChannelID, newTitle);
            }
            catch (Exception e)
            {
                await _commandResponder.Respond("Failed changing title while closing...");
                Log.Error(e, "Failed changing title of forum post");
            }
        }

        var topMessage = await _channelApi.GetChannelMessagesAsync(_commandContext.ChannelID, around: _commandContext.ChannelID, limit: 1);

        if (topMessage.IsSuccess && topMessage.Entity.FirstOrDefault() is { } messageToAction)
        {
            try
            {
                await _channelApi.DeleteAllReactionsAsync(_commandContext.ChannelID, messageToAction.ID);
                await _channelApi.CreateReactionAsync(_commandContext.ChannelID, messageToAction.ID, "✅");
            }
            catch (Exception e)
            {
                await _commandResponder.Respond("Failed changing reactions while closing...");
                Log.Error(e, "Failed changing reactions of forum post");
            }
        }

        await _commandResponder.Respond("✅ This post has been marked as answered!");

        try
        {
            await _channelApi.ModifyThreadChannelAsync(_commandContext.ChannelID, isArchived: true);
        }
        catch (Exception e)
        {
            await _commandResponder.Respond("Failed archiving while closing...");
            Log.Error(e, "Failed archiving forum post");
        }

        return Result.FromSuccess();
    }
}
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Helpers.Permissions;
using Accord.Services.UserHiddenChannels;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("channel"), AutoConstructor]
public partial class UserChannelHidingCommandGroup : AccordCommandGroup
{
    private readonly IMediator _mediator;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly FeedbackService _feedbackService;
    private readonly DiscordCache _discordCache;

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("self-destruct"), Description("Time to self destruct, baby!")]
    public async Task<IResult> RemoveFunctionality()
    {
        await _feedbackService.SendContextualAsync("Hold onto ye' butts, get ready for spam *sorry, not sorry*");
        
        var hiddenChannelsForUser = await _mediator.Send(new GetAllUsersHiddenChannelsRequest());

        if (!hiddenChannelsForUser.Any())
        {
            return await _feedbackService.SendContextualAsync("No hidden channels");
        }

        var grouped = hiddenChannelsForUser.GroupBy(x => x.UserId);

        var channels = await _discordCache.GetGuildChannels();

        foreach (var group in grouped)
        {
            await _feedbackService.SendContextualAsync($"Processing user {group.Key} with {group.Count()} channels hidden");
            var guildMember = await _discordCache.GetGuildMember(group.Key);

            foreach (var channelToShow in group)
            {
                await _feedbackService.SendContextualAsync($"Processing {channelToShow.DiscordChannelId} for user {group.Key}");
                var channel = channels.FirstOrDefault(x => x.ID.Value == channelToShow.DiscordChannelId);

                if (channel is null)
                {
                    await _feedbackService.SendContextualAsync($"Channel {channelToShow.DiscordChannelId} not found, skipping");
                    continue;
                }

                var userHasDenyPermission = channel.HasUserPermissionOverwrite(guildMember.Entity.User.Value, DiscordPermission.ViewChannel, DiscordPermissionType.Deny);
                

                if (userHasDenyPermission)
                {
                    await _feedbackService.SendContextualAsync("User has channel hidden via Discord perms");
                    
                    if (channel.Type == ChannelType.GuildCategory)
                    {
                        await _feedbackService.SendContextualAsync("Channel is a category.... *sigh*");
                        
                        foreach (var inheritedChannel in channels)
                        {
                            if (inheritedChannel.ParentID == channel.ID)
                            {
                                if (inheritedChannel.HasUserPermissionOverwrite(guildMember.Entity.User.Value, DiscordPermission.ViewChannel, DiscordPermissionType.Deny))
                                    await _channelApi.DeleteChannelPermissionAsync(inheritedChannel.ID, guildMember.Entity.User.Value.ID);
                            }
                        }
                    }
                    else
                    {
                        await _channelApi.DeleteChannelPermissionAsync(channel.ID, guildMember.Entity.User.Value.ID);
                        await _feedbackService.SendContextualAsync("Channel override removed");
                    }
                }
            }
        }

        return await _feedbackService.SendContextualAsync("Natural order has been returned");
    }
}
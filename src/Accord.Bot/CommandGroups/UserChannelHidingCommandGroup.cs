using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Helpers.Permissions;
using Accord.Bot.Parsers;
using Accord.Services.Helpers;
using Accord.Services.UserHiddenChannels;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("channel"), AutoConstructor]
public partial class UserChannelHidingCommandGroup: AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IMediator _mediator;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly FeedbackService _feedbackService;
    private readonly DiscordCache _discordCache;
    private readonly DiscordAvatarHelper _discordAvatarHelper;
    private readonly DiscordPermissionHelper _discordPermissionHelper;
    private readonly ILogger<UserChannelHidingCommandGroup> _logger;

    [Command("hidden"), Description("Display your hidden channels"), Ephemeral]
    public async Task<IResult> GetHiddenChannels()
    {
        var proxy = _commandContext.GetCommandProxy();

        var hiddenChannelsForUser = await _mediator.Send(new GetUserHiddenChannelsRequest(proxy.UserId.Value));

        if (!hiddenChannelsForUser.Any())
        {
            return await _feedbackService.SendContextualAsync("You don't have any hidden channel yet");
        }

        StringBuilder stringBuilder = new();
        
        foreach (var blockedChannel in hiddenChannelsForUser.Where(x => x.ParentDiscordChannelId == null))
        {
            stringBuilder.AppendLine($"{DiscordFormatter.ChannelIdToMention(blockedChannel.DiscordChannelId)}");
        }

        var userRequest = await _discordCache.GetGuildMember(proxy.UserId.Value);

        if (!userRequest.IsSuccess)
        {
            return await _feedbackService.SendContextualAsync("Failed listing hidden channels");
        }

        var user = userRequest.Entity.User.Value;

        return await _feedbackService.SendContextualEmbedAsync(new Embed
        {
            Author = new EmbedAuthor(
                DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator),
                IconUrl: _discordAvatarHelper.GetAvatarUrl(user.ID.Value, 
                    user.Discriminator, 
                    user.Avatar?.Value, 
                    user.Avatar?.HasGif == true)),
            Title = "Hidden Channels",
            Description = stringBuilder.ToString()
        });
    }

    [Command("hide"), Description("Hide a channel for you"), Ephemeral]
    public async Task<IResult> HideChannel(IChannel channel)
    {
        var proxy = _commandContext.GetCommandProxy();

        var guildMember = await _discordCache.GetGuildMember(proxy.UserId.Value);

        if (!guildMember.IsSuccess)
            return await _feedbackService.SendContextualAsync("There was an error executing this command. Try again later.");

        var activeHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(proxy.UserId.Value));
        if (activeHiddenChannels.Any(x => x.DiscordChannelId == channel.ID.Value))
            return await _feedbackService.SendContextualAsync("This channel is already hidden for you.");

        var isCascadeEnabled = await _mediator.Send(new GetIsUserHiddenChannelsCascadeHideEnabledRequest());
        var hasUserPermissionToViewTheChannel = await _discordPermissionHelper.HasUserEffectivePermissionsInChannel(
            guildMember.Entity,
            channel,
            DiscordPermission.ViewChannel);
        var hasBotPermissionToManageTheChannel = await _discordPermissionHelper.HasBotEffectivePermissionsInChannel(
            channel,
            DiscordPermission.ViewChannel, DiscordPermission.ManageRoles);

        if (!hasBotPermissionToManageTheChannel || !hasUserPermissionToViewTheChannel)
            return await _feedbackService.SendContextualAsync($"This channel cannot be hidden!");

        var resultList = new List<(ulong Channel, Result Result)>
        {
            (
                channel.ID.Value,
                await _channelApi.EditChannelPermissionsAsync(
                    channel.ID,
                    proxy.UserId,
                    type: PermissionOverwriteType.Member,
                    deny: new DiscordPermissionSet(DiscordPermission.ViewChannel))
            )
        };

        if (channel.Type == ChannelType.GuildCategory && isCascadeEnabled)
        {
            var channels = await _discordCache.GetGuildChannels();

            foreach (var inheritedChannel in channels)
            {
                if (inheritedChannel.ParentID == channel.ID)
                {
                    var hasUserPermissionToViewTheInheritedChannel = await _discordPermissionHelper.HasUserEffectivePermissionsInChannel(
                        guildMember.Entity,
                        inheritedChannel,
                        DiscordPermission.ViewChannel);

                    var hasBotPermissionToManageTheInheritedChannel =
                        await _discordPermissionHelper.HasBotEffectivePermissionsInChannel(
                            inheritedChannel,
                            DiscordPermission.ViewChannel, DiscordPermission.ManageRoles);

                    if (!hasBotPermissionToManageTheInheritedChannel || !hasUserPermissionToViewTheInheritedChannel)
                        continue;

                    if (activeHiddenChannels.Any(x => x.DiscordChannelId == inheritedChannel.ID.Value))
                        resultList.Add((inheritedChannel.ID.Value, Result.FromSuccess()));
                    else
                        resultList.Add((
                            inheritedChannel.ID.Value,
                            await _channelApi.EditChannelPermissionsAsync(
                                inheritedChannel.ID,
                                proxy.UserId,
                                type: PermissionOverwriteType.Member,
                                deny: new DiscordPermissionSet(DiscordPermission.ViewChannel))
                        ));
                }
            }

            var successfulHiddenChannels = resultList.Where(x => x.Result.IsSuccess).ToList();
            var failedHiddenChannels = resultList.Except(successfulHiddenChannels).ToList();

            if (successfulHiddenChannels.Count != resultList.Count)
            {
                await _feedbackService.SendContextualAsync(
                    $"{successfulHiddenChannels.Count}/{resultList.Count} channels in the category {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} have been hidden.");

                var errors = string.Join("\n\t", failedHiddenChannels.Select(x => x.Result.Error!.Message));

                _logger.LogWarning("Hiding Channels under the category {Category} for user: {User}#{Discriminator}({Id}) produced the following errors:\n{Errors}",
                    channel.Name.Value,
                    guildMember.Entity.User.Value.Username,
                    guildMember.Entity.User.Value.Discriminator,
                    guildMember.Entity.User.Value.ID.Value,
                    errors);
            }
            else
            {
                await _feedbackService.SendContextualAsync($"All the channels in the category: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} have been hidden.");
            }

            await _mediator.Send(new AddUserHiddenChannelsRequest(
                proxy.UserId.Value,
                channel.ID.Value,
                successfulHiddenChannels.Select(x => x.Channel).Where(x => x != channel.ID.Value).ToList()));
        }
        else
        {
            if (!resultList[0].Result.IsSuccess)
            {
                await _feedbackService.SendContextualAsync($"An error occured while hiding channel: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)}");
                _logger.LogWarning("Hiding channel {Channel} for user: {User}#{Discriminator}({Id}) produced the following error:\n\t{Error}",
                    channel.Name.Value,
                    guildMember.Entity.User.Value.Username,
                    guildMember.Entity.User.Value.Discriminator,
                    guildMember.Entity.User.Value.ID.Value,
                    resultList[0].Result.Error!.Message);
            }
            else
            {
                await _feedbackService.SendContextualAsync($"Channel {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} has been hidden");

                await _mediator.Send(new AddUserHiddenChannelRequest(
                    proxy.UserId.Value,
                    channel.ID.Value
                ));
            }
        }

        return Result.FromSuccess();
    }

    [Command("show"), Description("Show a channel you've hidden"), Ephemeral]
    public async Task<IResult> ShowChannel(string channelText)
    {
        var channels = await _discordCache.GetGuildChannels();
        IChannel? channel;

        if (!channels.Any())
        {
            await _feedbackService.SendContextualAsync("I found no channels in this Discord!");
            return Result.FromSuccess();
        }

        if(ulong.TryParse(channelText, out var channelId))
        {
            channel = channels
                .Where(x => x.ID.Value == channelId)
                .FirstOrDefault();
        }
        else
        {
            channel = channels
                .Where(x => string.Equals(x.Name.Value, channelText, System.StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
        }

        if (channel is null)
        {
            return await _feedbackService.SendContextualAsync($"Channel: {channelText} not found");
        }
        
        var proxy = _commandContext.GetCommandProxy();

        var isCascadeEnabled = await _mediator.Send(new GetIsUserHiddenChannelsCascadeHideEnabledRequest());

        var resultList = new List<(ulong Channel, Result Result)>();
        var activeUserHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(proxy.UserId.Value));
        
        var guildMember = await _discordCache.GetGuildMember(proxy.UserId.Value);

        var userHasDenyPermission = channel.HasUserPermissionOverwrite(guildMember.Entity.User.Value, DiscordPermission.ViewChannel, DiscordPermissionType.Deny);

        if (activeUserHiddenChannels.All(x => x.DiscordChannelId != channel.ID.Value) && !userHasDenyPermission)
            return await _feedbackService.SendContextualAsync("This channel is not hidden for you.");

        if (!userHasDenyPermission)
            resultList.Add((channel.ID.Value, Result.FromSuccess()));
        else
            resultList.Add((channel.ID.Value, await _channelApi.DeleteChannelPermissionAsync(channel.ID, proxy.UserId)));

        if (channel.Type == ChannelType.GuildCategory && isCascadeEnabled)
        {
            foreach (var inheritedChannel in channels)
            {
                if (inheritedChannel.ParentID == channel.ID)
                {
                    if (!inheritedChannel.HasUserPermissionOverwrite(guildMember.Entity.User.Value, DiscordPermission.ViewChannel, DiscordPermissionType.Deny))
                        resultList.Add((inheritedChannel.ID.Value, Result.FromSuccess()));
                    else
                        resultList.Add((inheritedChannel.ID.Value, await _channelApi.DeleteChannelPermissionAsync(inheritedChannel.ID, proxy.UserId)));
                }
            }

            var successfulShownChannels = resultList.Where(x => x.Result.IsSuccess).ToList();
            var failedShownChannels = resultList.Except(successfulShownChannels).ToList();

            if (successfulShownChannels.Count != resultList.Count)
            {
                await _feedbackService.SendContextualAsync(
                    $"{successfulShownChannels.Count}/{resultList.Count} channels in the category {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} are now visible.");

                var errors = string.Join("\n\t", failedShownChannels.Select(x => x.Result.Error!.Message));

                _logger.LogWarning("Showing Channels under the category {Category} for user: {User}#{Discriminator}({Id}) produced the following errors:\n{Errors}",
                    channel.Name.Value,
                    guildMember.Entity.User.Value.Username,
                    guildMember.Entity.User.Value.Discriminator,
                    guildMember.Entity.User.Value.ID.Value,
                    errors);
            }
            else
            {
                await _feedbackService.SendContextualAsync($"All the channels in the category: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} are now visible.");
            }

            var channelsToRemoveFromDb = successfulShownChannels.Where(x => activeUserHiddenChannels.Any(y => y.DiscordChannelId == x.Channel)).Select(x => x.Channel).ToList();
            await _mediator.Send(new DeleteUserHiddenChannelRequest(proxy.UserId.Value, channel.ID.Value, channelsToRemoveFromDb));
        }
        else
        {
            if (!resultList[0].Result.IsSuccess)
            {
                await _feedbackService.SendContextualAsync($"An error occured while showing channel: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)}");
                _logger.LogWarning("Showing channel {Channel} for user: {User}#{Discriminator}({Id}) produced the following error:\n\t{Error}",
                    channel.Name.Value,
                    guildMember.Entity.User.Value.Username,
                    guildMember.Entity.User.Value.Discriminator,
                    guildMember.Entity.User.Value.ID.Value,
                    resultList[0].Result.Error!.Message);
            }
            else
            {
                await _feedbackService.SendContextualAsync($"Channel {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} is now visible");
                await _mediator.Send(new DeleteUserHiddenChannelRequest(proxy.UserId.Value, channel.ID.Value));
            }
        }

        return Result.FromSuccess();
    }
}
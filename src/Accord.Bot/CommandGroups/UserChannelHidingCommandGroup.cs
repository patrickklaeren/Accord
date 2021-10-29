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
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("channel")]
public class UserChannelHidingCommandGroup: AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IMediator _mediator;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly CommandResponder _commandResponder;
    private readonly DiscordScopedCache _discordCache;
    private readonly DiscordAvatarHelper _discordAvatarHelper;
    private readonly DiscordPermissionHelper _discordPermissionHelper;
    private readonly DiscordChannelParser _discordChannelParser;
    private readonly ILogger<UserChannelHidingCommandGroup> _logger;

    public UserChannelHidingCommandGroup(ICommandContext commandContext,
        IMediator mediator,
        IDiscordRestChannelAPI channelApi,
        CommandResponder commandResponder,
        DiscordAvatarHelper discordAvatarHelper,
        DiscordScopedCache discordCache,
        DiscordPermissionHelper discordPermissionHelper,
        ILogger<UserChannelHidingCommandGroup> logger,
        DiscordChannelParser discordChannelParser)
    {
        _commandContext = commandContext;
        _mediator = mediator;
        _channelApi = channelApi;
        _commandResponder = commandResponder;
        _discordAvatarHelper = discordAvatarHelper;
        _discordCache = discordCache;
        _discordPermissionHelper = discordPermissionHelper;
        _logger = logger;
        _discordChannelParser = discordChannelParser;
    }

    [Command("hidden"), Description("Display your hidden channels")]
    public async Task<IResult> GetHiddenChannels()
    {
        var hiddenChannelsForUser = await _mediator.Send(new GetUserHiddenChannelsRequest(_commandContext.User.ID.Value));

        if (!hiddenChannelsForUser.Any())
        {
            return await _commandResponder.Respond("You don't have any hidden channel yet");
        }

        StringBuilder sb = new();
        foreach (var blockedChannel in hiddenChannelsForUser.Where(x => x.ParentDiscordChannelId == null))
        {
            sb.AppendLine($"{DiscordFormatter.ChannelIdToMention(blockedChannel.DiscordChannelId)}");
        }

        return await _commandResponder.Respond(new Embed
        {
            Author = new EmbedAuthor(
                DiscordHandleHelper.BuildHandle(_commandContext.User.Username, _commandContext.User.Discriminator),
                IconUrl: _discordAvatarHelper.GetAvatarUrl(_commandContext.User)),
            Title = "Hidden Channels",
            Description = sb.ToString()
        });
    }

    [Command("hide"), Description("Hide a channel for you")]
    public async Task<IResult> HideChannel(IChannel channel)
    {
        var guildMember = await _discordCache.GetInvokingGuildMember();
        if (!guildMember.IsSuccess || guildMember.Entity == null)
            return await _commandResponder.Respond("There was an error executing this command. Try again later.");

        var activeHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(_commandContext.User.ID.Value));
        if (activeHiddenChannels.Any(x => x.DiscordChannelId == channel.ID.Value))
            return await _commandResponder.Respond("This channel is already hidden for you.");

        var isCascadeEnabled = await _mediator.Send(new GetIsUserHiddenChannelsCascadeHideEnabledRequest());
        var hasUserPermissionToViewTheChannel = _discordPermissionHelper.HasUserEffectivePermissionsInChannel(
            _commandContext.GuildID.Value.Value,
            guildMember.Entity,
            channel,
            DiscordPermission.ViewChannel);
        var hasBotPermissionToManageTheChannel = _discordPermissionHelper.HasBotEffectivePermissionsInChannel(
            _commandContext.GuildID.Value.Value,
            channel,
            DiscordPermission.ViewChannel, DiscordPermission.ManageRoles);

        if (!hasBotPermissionToManageTheChannel || !hasUserPermissionToViewTheChannel)
            return await _commandResponder.Respond($"This channel cannot be hidden!");

        var resultList = new List<(ulong Channel, Result Result)>
        {
            (
                channel.ID.Value,
                await _channelApi.EditChannelPermissionsAsync(
                    channel.ID,
                    _commandContext.User.ID,
                    type: PermissionOverwriteType.Member,
                    deny: new DiscordPermissionSet(DiscordPermission.ViewChannel))
            )
        };

        if (channel.Type == ChannelType.GuildCategory && isCascadeEnabled)
        {
            var channels = _discordCache.GetGuildChannels();
            foreach (var inheritedChannel in channels)
            {
                if (inheritedChannel.ParentID == channel.ID)
                {
                    var hasUserPermissionToViewTheInheritedChannel = _discordPermissionHelper.HasUserEffectivePermissionsInChannel(
                        _commandContext.GuildID.Value.Value,
                        guildMember.Entity,
                        inheritedChannel,
                        DiscordPermission.ViewChannel);
                    var hasBotPermissionToManageTheInheritedChannel =
                        _discordPermissionHelper.HasBotEffectivePermissionsInChannel(
                            _commandContext.GuildID.Value.Value,
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
                                _commandContext.User.ID,
                                type: PermissionOverwriteType.Member,
                                deny: new DiscordPermissionSet(DiscordPermission.ViewChannel))
                        ));
                }
            }

            var successfulHiddenChannels = resultList.Where(x => x.Result.IsSuccess).ToList();
            var failedHiddenChannels = resultList.Except(successfulHiddenChannels).ToList();

            if (successfulHiddenChannels.Count != resultList.Count)
            {
                await _commandResponder.Respond(
                    $"{successfulHiddenChannels.Count}/{resultList.Count} channels in the category {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} have been hidden.");

                var errors = string.Join("\n\t", failedHiddenChannels.Select(x => x.Result.Error!.Message));

                _logger.LogWarning("Hiding Channels under the category {Category} for user: {User}#{Discriminator}({Id}) produced the following errors:\n{Errors}",
                    channel.Name.Value,
                    _commandContext.User.Username,
                    _commandContext.User.Discriminator,
                    _commandContext.User.ID.Value,
                    errors);
            }
            else
            {
                await _commandResponder.Respond($"All the channels in the category: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} have been hidden.");
            }

            await _mediator.Send(new AddUserHiddenChannelsRequest(
                _commandContext.User.ID.Value,
                channel.ID.Value,
                successfulHiddenChannels.Select(x => x.Channel).Where(x => x != channel.ID.Value).ToList()));
        }
        else
        {
            if (!resultList[0].Result.IsSuccess)
            {
                await _commandResponder.Respond($"An error occured while hiding channel: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)}");
                _logger.LogWarning("Hiding channel {Channel} for user: {User}#{Discriminator}({Id}) produced the following error:\n\t{Error}",
                    channel.Name.Value,
                    _commandContext.User.Username,
                    _commandContext.User.Discriminator,
                    _commandContext.User.ID.Value,
                    resultList[0].Result.Error!.Message);
            }
            else
            {
                await _commandResponder.Respond($"Channel {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} has been hidden");

                await _mediator.Send(new AddUserHiddenChannelRequest(
                    _commandContext.User.ID.Value,
                    channel.ID.Value
                ));
            }
        }

        return Result.FromSuccess();
    }

    [Command("show"), Description("Show a channel you've hidden")]
    public async Task<IResult> ShowChannel(string channelText)
    {
        var results = _discordChannelParser.TryParse(channelText);
        if (!results.IsSuccess)
            return await _commandResponder.Respond($"Channel: {channelText} not found");

        IChannel? channel;

        if (results.Entity.Channels.Count == 1)
        {
            channel = results.Entity.Channels[0];
        }
        else if (results.Entity is ApproximateChannelParsingResults approximateChannelParsingResults)
        {
            var minimumScore = approximateChannelParsingResults.Scores.Min(x => x.Value);
            var result = approximateChannelParsingResults.Scores
                .OrderBy(x => x.Value)
                .FirstOrDefault(x => x.Value == minimumScore
                                     && x.Key.HasUserPermissionOverwrite(_commandContext.User, DiscordPermission.ViewChannel, DiscordPermissionType.Deny))
                .Key;
            channel = result;
        }
        else
        {
            var result = results.Entity.Channels
                .FirstOrDefault(x => x.HasUserPermissionOverwrite(_commandContext.User, DiscordPermission.ViewChannel, DiscordPermissionType.Deny));
                
            channel = result;
        }

        if (channel == null)
            return await _commandResponder.Respond($"Channel: {channelText} not found");

        var isCascadeEnabled = await _mediator.Send(new GetIsUserHiddenChannelsCascadeHideEnabledRequest());

        var resultList = new List<(ulong Channel, Result Result)>();
        var activeUserHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(_commandContext.User.ID.Value));

        var userHasDenyPermission = channel.HasUserPermissionOverwrite(_commandContext.User, DiscordPermission.ViewChannel, DiscordPermissionType.Deny);

        if (activeUserHiddenChannels.All(x => x.DiscordChannelId != channel.ID.Value) && !userHasDenyPermission)
            return await _commandResponder.Respond("This channel is not hidden for you.");

        if (!userHasDenyPermission)
            resultList.Add((channel.ID.Value, Result.FromSuccess()));
        else
            resultList.Add((channel.ID.Value, await _channelApi.DeleteChannelPermissionAsync(channel.ID, _commandContext.User.ID)));

        if (channel.Type == ChannelType.GuildCategory && isCascadeEnabled)
        {
            var channels = _discordCache.GetGuildChannels();
            foreach (var inheritedChannel in channels)
            {
                if (inheritedChannel.ParentID == channel.ID)
                {
                    if (!inheritedChannel.HasUserPermissionOverwrite(_commandContext.User, DiscordPermission.ViewChannel, DiscordPermissionType.Deny))
                        resultList.Add((inheritedChannel.ID.Value, Result.FromSuccess()));
                    else
                        resultList.Add((inheritedChannel.ID.Value, await _channelApi.DeleteChannelPermissionAsync(inheritedChannel.ID, _commandContext.User.ID)));
                }
            }

            var successfulShownChannels = resultList.Where(x => x.Result.IsSuccess).ToList();
            var failedShownChannels = resultList.Except(successfulShownChannels).ToList();

            if (successfulShownChannels.Count != resultList.Count)
            {
                await _commandResponder.Respond(
                    $"{successfulShownChannels.Count}/{resultList.Count} channels in the category {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} are now visible.");

                var errors = string.Join("\n\t", failedShownChannels.Select(x => x.Result.Error!.Message));

                _logger.LogWarning("Showing Channels under the category {Category} for user: {User}#{Discriminator}({Id}) produced the following errors:\n{Errors}",
                    channel.Name.Value,
                    _commandContext.User.Username,
                    _commandContext.User.Discriminator,
                    _commandContext.User.ID.Value,
                    errors);
            }
            else
            {
                await _commandResponder.Respond($"All the channels in the category: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} are now visible.");
            }

            var channelsToRemoveFromDb = successfulShownChannels.Where(x => activeUserHiddenChannels.Any(y => y.DiscordChannelId == x.Channel)).Select(x => x.Channel).ToList();
            await _mediator.Send(new DeleteUserHiddenChannelRequest(_commandContext.User.ID.Value, channel.ID.Value, channelsToRemoveFromDb));
        }
        else
        {
            if (!resultList[0].Result.IsSuccess)
            {
                await _commandResponder.Respond($"An error occured while showing channel: {DiscordFormatter.ChannelIdToMention(channel.ID.Value)}");
                _logger.LogWarning("Showing channel {Channel} for user: {User}#{Discriminator}({Id}) produced the following error:\n\t{Error}",
                    channel.Name.Value,
                    _commandContext.User.Username,
                    _commandContext.User.Discriminator,
                    _commandContext.User.ID.Value,
                    resultList[0].Result.Error!.Message);
            }
            else
            {
                await _commandResponder.Respond($"Channel {DiscordFormatter.ChannelIdToMention(channel.ID.Value)} is now visible");
                await _mediator.Send(new DeleteUserHiddenChannelRequest(_commandContext.User.ID.Value, channel.ID.Value));
            }
        }

        return Result.FromSuccess();
    }
}
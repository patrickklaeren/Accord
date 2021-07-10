using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.UserHiddenChannels;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Parsers;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    [Group("channel")]
    public class UserChannelHidingCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly CommandResponder _commandResponder;
        private readonly DiscordCache _discordCache;
        private readonly DiscordAvatarHelper _discordAvatarHelper;

        public UserChannelHidingCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi,
            CommandResponder commandResponder,
            DiscordCache discordCache,
            DiscordAvatarHelper discordAvatarHelper)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _channelApi = channelApi;
            _guildApi = guildApi;
            _commandResponder = commandResponder;
            _discordCache = discordCache;
            _discordAvatarHelper = discordAvatarHelper;
        }

        [RequireContext(ChannelContext.Guild), Command("hidden"), Description("Display your hidden channels")]
        public async Task<IResult> GetHiddenChannels()
        {
            var hiddenChannelsForUser = await _mediator.Send(new GetUserHiddenChannelsRequest(_commandContext.User.ID.Value));

            if (!hiddenChannelsForUser.Any())
            {
                return await _commandResponder.Respond("You don't have any hidden channel yet");
            }

            StringBuilder sb = new();
            foreach (var blockedChannel in hiddenChannelsForUser)
            {
                sb.AppendLine($"{DiscordFormatter.ChannelIdToMention(blockedChannel)}");
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

        [RequireContext(ChannelContext.Guild), Command("hide"), Description("Hide a channel for you")]
        public async Task<IResult> HideChannel(IChannel channel)
        {
            var actionResult = await _mediator.Send(new AddUserHiddenChannelRequest(_commandContext.User.ID.Value, channel.ID.Value));

            if (actionResult.Failure)
            {
                await _commandResponder.Respond(actionResult.ErrorMessage);
                return Result.FromSuccess();
            }

            var isCascadeEnabled = await _mediator.Send(new GetIsUserHiddenChannelsCascadeHideEnabledRequest());

            var resultList = new List<(ulong Channel, Result Result)>();
            if (channel.Type == ChannelType.GuildCategory && isCascadeEnabled)
            {
                var channels = await _discordCache.GetChannels(_commandContext.GuildID.Value.Value);
                foreach (var inheritedChannel in channels.Entity)
                {
                    if (inheritedChannel.ParentID == channel.ID)
                    {
                        resultList.Add((
                            inheritedChannel.ID.Value,
                            await _channelApi.EditChannelPermissionsAsync(inheritedChannel.ID, _commandContext.User.ID, type: PermissionOverwriteType.Member,
                                deny: new DiscordPermissionSet(DiscordPermission.ViewChannel))
                        ));
                    }
                }
            }

            resultList.Add((channel.ID.Value,
                    await _channelApi.EditChannelPermissionsAsync(channel.ID, _commandContext.User.ID, type: PermissionOverwriteType.Member,
                        deny: new DiscordPermissionSet(DiscordPermission.ViewChannel))
                ));


            if (!resultList.Select(x => x.Result.IsSuccess).All(x => x))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var result in resultList.Where(x => !x.Result.IsSuccess))
                {
                    sb.AppendLine($"|-- Couldn't hide channel: {result.Channel} -> {result.Result.Error!.Message}");
                }

                await _commandResponder.Respond($"There was an error hiding these channel(s) for you. Try again later.\n{sb}");
            }
            else
            {
                await _commandResponder.Respond($"Channel {DiscordFormatter.ChannelIdToMention(channel.ID.Value!)} is now hidden");
            }

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("show"), Description("Show a channel you've hidden")]
        public async Task<IResult> ShowChannel(String channelText)
        {
            IChannel? channel = null;
            var parsingResult = await new ChannelParser(_channelApi).TryParse(channelText, default);
            if (parsingResult.IsSuccess)
            {
                channel = parsingResult.Entity;
            }
            else
            {
                var channelsResult = await _guildApi.GetGuildChannelsAsync(_commandContext.GuildID.Value);
                if (channelsResult.IsSuccess)
                {
                    var result = channelsResult.Entity
                                     .SingleOrDefault(x => x.PermissionOverwrites.HasValue
                                                           && x.PermissionOverwrites.Value.Any(y => y.ID.Value == x.ID.Value)
                                                           && String.Equals(x.Name.Value!.Replace("#", ""), channelText.Replace("#", ""),
                                                               StringComparison.InvariantCultureIgnoreCase)
                                     )
                                 ?? channelsResult.Entity
                                     .Select(x => new
                                     {
                                         Channel = x,
                                         Score = channelText
                                             .Replace("#", "")
                                             .GetDamerauLevenshteinDistance(x.Name.Value!.Replace("#", ""))
                                     })
                                     .OrderBy(x => x.Score)
                                     .FirstOrDefault(x => x.Score <= 2)?.Channel;

                    if (result != null)
                    {
                        channel = result;
                    }
                    else if (ulong.TryParse(channelText, out var channelId))
                    {
                        channel = channelsResult.Entity.SingleOrDefault(x => x.ID.Value == channelId);
                    }
                }
            }

            if (channel == null)
            {
                await _commandResponder.Respond($"Channel: {channelText} not found");
                return Result.FromSuccess();
            }

            if (channel.PermissionOverwrites.HasValue &&
                !channel.PermissionOverwrites.Value.Any(x => x.Deny.HasPermission(DiscordPermission.ViewChannel) && x.ID.Value == _commandContext.User.ID.Value))
            {
                await _commandResponder.Respond(
                    $"{DiscordFormatter.ChannelIdToMention(channel.ID.Value!)} should visible to you, unless you didn't have access to it in the first place.");
                return Result.FromSuccess();
            }


            var actionResult = await _mediator.Send(new DeleteUserHiddenChannelRequest(_commandContext.User.ID.Value, channel.ID.Value));

            if (actionResult.Failure)
            {
                await _commandResponder.Respond(actionResult.ErrorMessage);
                return Result.FromSuccess();
            }

            var isCascadeEnabled = await _mediator.Send(new GetIsUserHiddenChannelsCascadeHideEnabledRequest());

            var resultList = new List<Result>();
            if (channel.Type == ChannelType.GuildCategory && isCascadeEnabled)
            {
                var channels = await _discordCache.GetChannels(_commandContext.GuildID.Value.Value);
                foreach (var inheritedChannel in channels.Entity)
                {
                    if (inheritedChannel.ParentID == channel.ID)
                    {
                        resultList.Add(await _channelApi.DeleteChannelPermissionAsync(inheritedChannel.ID, _commandContext.User.ID));
                    }
                }
            }

            resultList.Add(await _channelApi.DeleteChannelPermissionAsync(channel.ID, _commandContext.User.ID));


            if (!resultList.Select(x => x.IsSuccess).All(x => x))
            {
                await _commandResponder.Respond("There was an error showing this channel for you. Try again later.");
            }
            else
            {
                await _commandResponder.Respond(
                    $"Channel {DiscordFormatter.ChannelIdToMention(channel.ID.Value!)} should visible to you, unless you didn't have access to it in the first place.");
            }

            return Result.FromSuccess();
        }
    }
}
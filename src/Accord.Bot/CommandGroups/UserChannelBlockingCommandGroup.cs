using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.UserChannelBlocks;
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
    public class UserChannelBlockingCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly CommandResponder _commandResponder;
        private readonly DiscordCache _discordCache;
        private readonly DiscordAvatarHelper _discordAvatarHelper;

        public UserChannelBlockingCommandGroup(ICommandContext commandContext,
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
            var usersBlockedChannels = await _mediator.Send(new GetUserBlockedChannelsRequest(_commandContext.User.ID.Value));

            if (usersBlockedChannels.Count == 0)
            {
                await _commandResponder.Respond("You don't have any hidden channel yet");
            }
            else
            {
                StringBuilder sb = new();
                foreach (var blockedChannel in usersBlockedChannels)
                {
                    sb.AppendLine($"{DiscordFormatter.ChannelIdToMention(blockedChannel)}");
                }

                await _commandResponder.Respond(new Embed
                {
                    Author = new EmbedAuthor(
                        DiscordHandleHelper.BuildHandle(_commandContext.User.Username, _commandContext.User.Discriminator),
                        IconUrl: _discordAvatarHelper.GetAvatarUrl(_commandContext.User)),
                    Title = "Hidden Channels",
                    Description = sb.ToString()
                });
            }

            return Result.FromSuccess();
        }


        [RequireContext(ChannelContext.Guild), Command("hide"), Description("Hide a channel for you")]
        public async Task<IResult> HideChannel(IChannel channel)
        {
            var actionResult = await _mediator.Send(new AddUserBlockedChannelRequest(_commandContext.User.ID.Value, channel.ID.Value));

            if (actionResult.Failure)
            {
                await _commandResponder.Respond(actionResult.ErrorMessage);
                return Result.FromSuccess();
            }

            var result = await _channelApi.EditChannelPermissionsAsync(channel.ID, _commandContext.User.ID, type: PermissionOverwriteType.Member,
                deny: new DiscordPermissionSet(DiscordPermission.ViewChannel));

            if (!result.IsSuccess)
            {
                await _commandResponder.Respond("There was an error hiding this channel for you. Try again later.");
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
                                                           && String.Equals(x.Name.Value.Replace("#", ""), channelText.Replace("#", ""),
                                                               StringComparison.InvariantCultureIgnoreCase)
                                     )
                                 ?? channelsResult.Entity
                                     .Select(x => new
                                     {
                                         Channel = x,
                                         Score = channelText
                                             .Replace("#", "")
                                             .GetDamerauLevenshteinDistance(x.Name.Value.Replace("#", ""))
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

            var actionResult = await _mediator.Send(new DeleteUserBlockedChannelRequest(_commandContext.User.ID.Value, channel.ID.Value));

            if (actionResult.Failure)
            {
                await _commandResponder.Respond(actionResult.ErrorMessage);
                return Result.FromSuccess();
            }

            await _channelApi.DeleteChannelPermissionAsync(channel.ID, _commandContext.User.ID);

            await _commandResponder.Respond(
                $"{DiscordFormatter.ChannelIdToMention(channel.ID.Value!)} should visible to you, unless you didn't have access to it in the first place.");

            return Result.FromSuccess();
        }
    }
}
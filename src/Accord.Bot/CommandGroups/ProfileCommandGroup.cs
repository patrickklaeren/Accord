using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.Users;
using Humanizer;
using MediatR;
using Polly.Caching;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    public class ProfileCommandGroup : CommandGroup
    {
        private readonly IMediator _mediator;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly DiscordAvatarHelper _discordAvatarHelper;
        private readonly CommandResponder _commandResponder;

        public ProfileCommandGroup(IMediator mediator, ICommandContext commandContext,
            IDiscordRestGuildAPI guildApi,
            DiscordAvatarHelper discordAvatarHelper,
            CommandResponder commandResponder)
        {
            _mediator = mediator;
            _commandContext = commandContext;
            _guildApi = guildApi;
            _discordAvatarHelper = discordAvatarHelper;
            _commandResponder = commandResponder;
        }

        [RequireContext(ChannelContext.Guild), Command("profile"), Description("Get your profile")]
        public async Task<IResult> GetProfile(IGuildMember? member = null)
        {
            if (member is not null && !member.User.HasValue)
            {
                await _commandResponder.Respond("Failed finding user");
                Result.FromSuccess();
            }

            var userId = member?.User.Value!.ID.Value ?? _commandContext.User.ID.Value;

            var response = await _mediator.Send(new GetUserRequest(userId));

            if (!response.Success)
            {
                await _commandResponder.Respond(response.ErrorMessage);
                return Result.FromSuccess();
            }

            var guildUserEntity = await _guildApi.GetGuildMemberAsync(_commandContext.GuildID.Value, new Snowflake(userId));

            if (!guildUserEntity.IsSuccess || guildUserEntity.Entity is null || !guildUserEntity.Entity.User.HasValue)
            {
                await _commandResponder.Respond("Couldn't find user in Guild");
                return Result.FromSuccess();
            }

            var guildUser = guildUserEntity.Entity;
            var (userDto, userMessagesInChannelDtos, userVoiceMinutesInChannelDtos) = response.Value!;

            var avatarUrl = _discordAvatarHelper.GetAvatarUrl(guildUser.User.Value);
            var avatarImage = _discordAvatarHelper.GetAvatar(guildUser.User.Value);

            var builder = new StringBuilder();

            var userHandle = !string.IsNullOrWhiteSpace(userDto.UsernameWithDiscriminator)
                ? userDto.UsernameWithDiscriminator
                : DiscordHandleHelper.BuildHandle(guildUser.User.Value.Username, guildUser.User.Value.Discriminator); 

            builder
                .AppendLine("**User Information**")
                .AppendLine($"ID: {userDto.Id}")
                .AppendLine($"Profile: {DiscordMentionHelper.UserIdToMention(userDto.Id)}")
                .AppendLine($"Handle: {userHandle}");

            if (!string.IsNullOrWhiteSpace(userDto.Nickname))
            {
                builder.AppendLine($"Nickname: {userDto.Nickname}");
            }

            if (userDto.JoinedGuildDateTime is not null)
            {
                var joinedSince = DateTimeOffset.Now - userDto.JoinedGuildDateTime.Value;
                builder.AppendLine($"Joined: {joinedSince.Humanize()} ago ({userDto.JoinedGuildDateTime.Value})");
            }

            var firstSeenSince = DateTimeOffset.Now - userDto.FirstSeenDateTime;
            builder.AppendLine($"First tracked: {firstSeenSince.Humanize()} ago ({userDto.FirstSeenDateTime})");

            builder
                .AppendLine()
                .AppendLine("**Guild Participation**")
                .AppendLine($"Rank: {userDto.ParticipationRank}")
                .AppendLine($"Points: {userDto.ParticipationPoints}")
                .AppendLine($"Percentile: {Math.Round(userDto.ParticipationPercentile, 1)}%")
                .AppendLine()
                .AppendLine("**Message Participation**")
                .AppendLine($"Last 30 days: {userMessagesInChannelDtos.Sum(x => x.NumberOfMessages)} messages");

            if (userMessagesInChannelDtos.Any())
            {
                var (discordChannelId, numberOfMessages) = userMessagesInChannelDtos
                    .OrderByDescending(x => x.NumberOfMessages)
                    .First();

                builder.AppendLine($"Most active text channel: {DiscordMentionHelper.ChannelIdToMention(discordChannelId)} ({numberOfMessages} messages)");
            }

            builder
                .AppendLine()
                .AppendLine("**Voice Participation**")
                .AppendLine($"Last 30 days: {TimeSpan.FromMinutes(userVoiceMinutesInChannelDtos.Sum(x => x.NumberOfMinutes)).Humanize()}");

            if (userVoiceMinutesInChannelDtos.Any())
            {
                var (discordChannelId, numberOfMinutes) = userVoiceMinutesInChannelDtos
                    .OrderByDescending(x => x.NumberOfMinutes)
                    .First();

                builder.AppendLine($"Most active voice channel: {DiscordMentionHelper.ChannelIdToMention(discordChannelId)} ({TimeSpan.FromMinutes(numberOfMinutes).Humanize()})");
            }

            var embed = new Embed(Author: new EmbedAuthor(userHandle, IconUrl: avatarUrl),
                Thumbnail: avatarImage,
                Description: builder.ToString());

            await _commandResponder.Respond(embed);

            return Result.FromSuccess();
        }
    }
}

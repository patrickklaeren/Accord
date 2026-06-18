using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.Users;
using Humanizer;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ProfileCommandGroup(IMediator mediator, ICommandContext commandContext, IDiscordRestGuildAPI guildApi, DiscordAvatarHelper discordAvatarHelper, FeedbackService feedbackService, ThumbnailHelper thumbnailHelper) : AccordCommandGroup
{
    [Command("profile"), Description("Get your profile")]
    public async Task<IResult> GetProfile(IGuildMember? member = null)
    {
        if (member is not null && !member.User.HasValue)
        {
            return await feedbackService.SendContextualAsync("Failed finding user");
        }

        var proxy = commandContext.GetCommandProxy();

        var userId = member?.User.Value!.ID.Value ?? proxy.UserId.Value;

        var response = await mediator.Send(new GetUserRequest(userId));

        if (!response.Success)
        {
            return await feedbackService.SendContextualAsync(response.ErrorMessage);
        }

        var guildUserEntity = await guildApi.GetGuildMemberAsync(proxy.GuildId, new Snowflake(userId));

        if (!guildUserEntity.IsSuccess || !guildUserEntity.Entity.User.HasValue)
        {
            return await feedbackService.SendContextualAsync("Couldn't find user in Guild");
        }

        var guildUser = guildUserEntity.Entity;
        var (userDto, userMessagesInChannelDtos, userVoiceMinutesInChannelDtos) = response.Value!;

        var avatarUrl = discordAvatarHelper.GetAvatarUrl(guildUser.User.Value.ID.Value,
            guildUser.User.Value.Discriminator,
            guildUser.User.Value.Avatar?.Value,
            guildUser.User.Value.Avatar?.HasGif == true);

        var avatarImage = thumbnailHelper.GetAvatar(guildUser.User.Value);

        var builder = new StringBuilder();

        var userHandle = !string.IsNullOrWhiteSpace(userDto.Username)
            ? userDto.Username
            : guildUser.User.Value.Username;

        var userCreated = DiscordSnowflakeHelper.ToDateTimeOffset(userId);

        builder
            .AppendLine("**User Information**")
            .AppendLine($"ID: {userDto.Id}")
            .AppendLine($"Profile: {DiscordFormatter.UserIdToMention(userDto.Id)}")
            .AppendLine($"Handle: {userHandle}");

        if (!string.IsNullOrWhiteSpace(userDto.Nickname))
        {
            builder.AppendLine($"Nickname: {userDto.Nickname}");
        }

        builder.AppendLine($"Created: {userCreated.ToTimeMarkdown()}");

        if (userDto.JoinedGuildDateTime is not null)
        {
            builder.AppendLine($"Joined: {userDto.JoinedGuildDateTime.Value.ToTimeMarkdown()}");
        }

        builder.AppendLine($"First tracked: {userDto.FirstSeenDateTime.ToTimeMarkdown()}");

        builder
            .AppendLine()
            .AppendLine("**Guild Participation**")
            .AppendLine($"Rank: {userDto.ParticipationRank}")
            .AppendLine($"Points: {userDto.ParticipationPoints}")
            .AppendLine($"Percentile: {Math.Round(userDto.ParticipationPercentile, 0)}")
            .AppendLine()
            .AppendLine("**Message Participation**")
            .AppendLine($"Last 30 days: {userMessagesInChannelDtos.Sum(x => x.NumberOfMessages)} messages");

        if (userMessagesInChannelDtos.Any())
        {
            var (discordChannelId, numberOfMessages) = userMessagesInChannelDtos
                .OrderByDescending(x => x.NumberOfMessages)
                .First();

            builder.AppendLine($"Most active text channel: {DiscordFormatter.ChannelIdToMention(discordChannelId)} ({numberOfMessages} messages)");
        }

        builder
            .AppendLine()
            .AppendLine("**Voice Participation**");

        if (userVoiceMinutesInChannelDtos.Any())
        {
            var (discordChannelId, numberOfMinutes) = userVoiceMinutesInChannelDtos
                .OrderByDescending(x => x.NumberOfMinutes)
                .First();

            builder
                .AppendLine($"Last 30 days: {TimeSpan.FromMinutes(userVoiceMinutesInChannelDtos.Sum(x => x.NumberOfMinutes)).Humanize()}")
                .AppendLine($"Most active voice channel: {DiscordFormatter.ChannelIdToMention(discordChannelId)} ({TimeSpan.FromMinutes(numberOfMinutes).Humanize()})");
        }
        else
        {
            builder
                .AppendLine("No time spent in voice channels");
        }

        var embed = new Embed(Author: new EmbedAuthor(userHandle, IconUrl: avatarUrl),
            Thumbnail: avatarImage,
            Description: builder.ToString());

        return await feedbackService.SendContextualEmbedAsync(embed);
    }
}
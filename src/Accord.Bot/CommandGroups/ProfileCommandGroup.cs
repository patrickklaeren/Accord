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
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ProfileCommandGroup(IMediator mediator, 
    ICommandContext commandContext, 
    IDiscordRestGuildAPI guildApi, 
    DiscordAvatarHelper discordAvatarHelper, 
    FeedbackService feedbackService, 
    ThumbnailHelper thumbnailHelper) 
    : AccordCommandGroup
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
        var (user, userHistory, userMessages, userVoiceActivity) = response.Value!;

        var avatarUrl = discordAvatarHelper.GetAvatarUrl(guildUser.User.Value.ID.Value,
            guildUser.User.Value.Discriminator,
            guildUser.User.Value.Avatar?.Value,
            guildUser.User.Value.Avatar?.HasGif == true);

        var avatarImage = thumbnailHelper.GetAvatar(guildUser.User.Value);

        var builder = new StringBuilder();

        var userHandle = !string.IsNullOrWhiteSpace(user.Username)
            ? user.Username
            : guildUser.User.Value.Username;

        var userCreated = DiscordSnowflakeHelper.ToDateTimeOffset(userId);

        builder
            .AppendLine("**User Information**")
            .AppendLine($"ID: {user.Id}")
            .AppendLine($"Profile: {DiscordFormatter.UserIdToMention(user.Id)}")
            .AppendLine($"Handle: {userHandle}");

        if (!string.IsNullOrWhiteSpace(user.Nickname))
        {
            builder.AppendLine($"Nickname: {user.Nickname}");
        }

        builder.AppendLine($"Created: {userCreated.ToTimeMarkdown()}");

        if (user.JoinedGuildDateTime is not null)
        {
            builder.AppendLine($"Joined: {user.JoinedGuildDateTime.Value.ToTimeMarkdown()}");
        }

        builder.AppendLine($"First tracked: {user.FirstSeenDateTime.ToTimeMarkdown()}");

        builder
            .AppendLine()
            .AppendLine("**Guild Participation**")
            .AppendLine($"Rank: {user.ParticipationRank}")
            .AppendLine($"Points: {user.ParticipationPoints}")
            .AppendLine($"Percentile: {Math.Round(user.ParticipationPercentile, 0)}")
            .AppendLine()
            .AppendLine("**Message Participation**")
            .AppendLine($"Last 30 days: {userMessages.Sum(x => x.NumberOfMessages)} messages");

        if (userMessages.Any())
        {
            var (discordChannelId, numberOfMessages) = userMessages
                .OrderByDescending(x => x.NumberOfMessages)
                .First();

            builder.AppendLine($"Most active text channel: {DiscordFormatter.ChannelIdToMention(discordChannelId)} ({numberOfMessages} messages)");
        }

        builder
            .AppendLine()
            .AppendLine("**Voice Participation**");

        if (userVoiceActivity.Any())
        {
            var (discordChannelId, numberOfMinutes) = userVoiceActivity
                .OrderByDescending(x => x.NumberOfMinutes)
                .First();

            builder
                .AppendLine($"Last 30 days: {TimeSpan.FromMinutes(userVoiceActivity.Sum(x => x.NumberOfMinutes)).Humanize()}")
                .AppendLine($"Most active voice channel: {DiscordFormatter.ChannelIdToMention(discordChannelId)} ({TimeSpan.FromMinutes(numberOfMinutes).Humanize()})");
        }
        else
        {
            builder
                .AppendLine("No time spent in voice channels");
        }

        builder
            .AppendLine()
            .AppendLine("**History**")
            .AppendLine($"{userHistory.Total} history records")
            .AppendLine($"{userHistory.Bans} bans, {userHistory.Kicks} kicks, {userHistory.Mutes} mutes, {userHistory.Warnings} warnings, {userHistory.Notes} notes");

        var embed = new Embed(Author: new EmbedAuthor(userHandle, IconUrl: avatarUrl),
            Thumbnail: avatarImage,
            Description: builder.ToString());

        return await feedbackService.SendContextualEmbedAsync(embed);
    }
}
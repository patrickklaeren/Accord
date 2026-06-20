using System;
using System.Collections.Generic;
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
    IDiscordRestUserAPI userApi,
    IDiscordRestGuildAPI guildApi,
    DiscordAvatarHelper discordAvatarHelper, 
    FeedbackService feedbackService, 
    ThumbnailHelper thumbnailHelper) 
    : AccordCommandGroup
{
    [Command("profile", "info"), Description("Get your profile")]
    public async Task<IResult> GetProfile(IUser? userToLookup = null)
    {
        var proxy = commandContext.GetCommandProxy();
        var userId = userToLookup?.ID ?? proxy.UserId;

        var discordUserResponse = await userApi.GetUserAsync(userId);

        if (!discordUserResponse.IsSuccess)
        {
            return await feedbackService.SendContextualAsync("Could not retrieve user via Discord API");
        }

        var discordUser = discordUserResponse.Entity;

        var response = await mediator.Send(new GetUserProfileRequest(userId.Value));
        
        UserDto? trackedUser = null;
        UserHistorySummaryDto? userHistory = null;
        IReadOnlyCollection<UserMessagesInChannelDto> userMessages = [];
        IReadOnlyCollection<UserVoiceMinutesInChannelDto> userVoiceActivity = [];

        if (response is not null)
        {
            (trackedUser, userHistory, userMessages, userVoiceActivity) = response;    
        }
        
        var status = await GetStatus(proxy.GuildId, userId, trackedUser);

        var avatarUrl = discordAvatarHelper.GetAvatarUrl(discordUser.ID.Value,
            discordUser.Discriminator,
            discordUser.Avatar?.Value,
            discordUser.Avatar?.HasGif == true);

        var avatarImage = thumbnailHelper.GetAvatar(discordUser);

        var builder = new StringBuilder();

        var userCreated = DiscordSnowflakeHelper.ToDateTimeOffset(userId.Value);

        builder
            .AppendLine("**User Information**")
            .AppendLine($"ID: {discordUser.ID.Value}")
            .AppendLine($"Profile: {DiscordFormatter.UserIdToMention(discordUser.ID.Value)}")
            .AppendLine($"Handle: {discordUser.Username}");
        
        if (!string.IsNullOrWhiteSpace(trackedUser?.Nickname))
        {
            builder.AppendLine($"Nickname: {trackedUser.Nickname}");
        }

        builder.AppendLine($"Created: {userCreated.ToTimeMarkdown()}");

        if (trackedUser?.JoinedGuildDateTime is not null)
        {
            builder.AppendLine($"Joined: {trackedUser.JoinedGuildDateTime.Value.ToTimeMarkdown()}");
            builder.AppendLine($"First tracked: {trackedUser.FirstSeenDateTime.ToTimeMarkdown()}");
        }
        
        builder.AppendLine($"Status: {status}");
        
        if (trackedUser is not null)
        {
            builder
                .AppendLine()
                .AppendLine("**Guild Participation**")
                .AppendLine($"Rank: {trackedUser.ParticipationRank}")
                .AppendLine($"Points: {trackedUser.ParticipationPoints}")
                .AppendLine($"Percentile: {Math.Round(trackedUser.ParticipationPercentile, 0)}")
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
                    .AppendLine("Last 30 days: No time spent in voice");
            }

            if (userHistory is not null)
            {
                builder
                    .AppendLine()
                    .AppendLine("**History**")
                    .AppendLine($"{userHistory.Total} history records")
                    .AppendLine($"{userHistory.Bans} bans, {userHistory.Kicks} kicks, {userHistory.Mutes} mutes, {userHistory.Warnings} warnings, {userHistory.Notes} notes");                
            }
        }

        var embed = new Embed(Author: new EmbedAuthor(discordUser.Username, IconUrl: avatarUrl),
            Thumbnail: avatarImage,
            Description: builder.ToString());

        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    private async Task<string> GetStatus(Snowflake guildSnowflake, 
        Snowflake userSnowflake, 
        UserDto? trackedUser)
    {
        var guildUser = await guildApi.GetGuildMemberAsync(guildSnowflake, userSnowflake);

        if (guildUser.IsSuccess)
        {
            return "Active in guild";
        }

        var ban = await guildApi.GetGuildBanAsync(guildSnowflake, userSnowflake);
            
        if (ban.IsSuccess)
        {
            return $"🔨 Banned for {ban.Entity.Reason}";
        }

        return trackedUser is not null ? "Left the guild" : "Has never joined the guild";
    }
}
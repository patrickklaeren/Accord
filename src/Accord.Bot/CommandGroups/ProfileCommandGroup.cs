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
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ProfileCommandGroup: AccordCommandGroup
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

    [Command("profile"), Description("Get your profile")]
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

        if (!guildUserEntity.IsSuccess || !guildUserEntity.Entity.User.HasValue)
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

        builder.AppendLine($"Created: {userCreated.ToDiscordDateMarkdown()}");

        if (userDto.JoinedGuildDateTime is not null)
        {
            builder.AppendLine($"Joined: {userDto.JoinedGuildDateTime.Value.ToDiscordDateMarkdown()}");
        }

        builder.AppendLine($"First tracked: {userDto.FirstSeenDateTime.ToDiscordDateMarkdown()}");

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

        await _commandResponder.Respond(embed);

        return Result.FromSuccess();
    }
}
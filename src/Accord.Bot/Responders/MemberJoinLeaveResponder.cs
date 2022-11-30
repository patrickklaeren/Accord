using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.Moderation;
using Accord.Services.Raid;
using Accord.Services.Users;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

[AutoConstructor]
public partial class MemberJoinLeaveResponder : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberRemove>
{
    private readonly IMediator _mediator;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IEventQueue _eventQueue;
    private readonly DiscordAvatarHelper _discordAvatarHelper;
    private readonly ThumbnailHelper _thumbnailHelper;

    public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (!gatewayEvent.User.HasValue)
            return Result.FromSuccess();

        var user = gatewayEvent.User.Value;

        var avatarUrl = _discordAvatarHelper.GetAvatarUrl(user.ID.Value, 
            user.Discriminator, 
            user.Avatar?.Value, 
            user.Avatar?.HasGif == true);

        await _eventQueue.Queue(new AddUserRequest(gatewayEvent.GuildID.Value, user.ID.Value, user.Username, user.Discriminator.ToPaddedDiscriminator(), avatarUrl, null, gatewayEvent.JoinedAt));
        await _eventQueue.Queue(new RaidCalculationRequest(gatewayEvent.GuildID.Value, new GuildUserDto(user.ID.Value, user.Username, user.Discriminator.ToPaddedDiscriminator(), null, avatarUrl, gatewayEvent.JoinedAt)));

        var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.JoinLeaveLogs), ct);

        if (channels.Any())
        {
            var image = _thumbnailHelper.GetAvatar(user);

            var builder = new StringBuilder()
                .AppendLine("**User Information**")
                .AppendLine($"ID: {user.ID.Value}")
                .AppendLine($"Profile: {user.ID.ToUserMention()}")
                .AppendLine($"Handle: {DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)}")
                .AppendLine($"Created: {DiscordSnowflakeHelper.ToDateTimeOffset(user.ID.Value):yyy-MM-dd HH:mm:ss}");

            var embed = new Embed(Title: $"{DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)} joined",
                Description: builder.ToString(),
                Thumbnail: image,
                Footer: new EmbedFooter($"{gatewayEvent.JoinedAt:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channels)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: ct);
            }
        }

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.JoinLeaveLogs), ct);

        var image = _thumbnailHelper.GetAvatar(gatewayEvent.User);

        var user = gatewayEvent.User;

        var builder = new StringBuilder()
            .AppendLine("**User Information**")
            .AppendLine($"ID: {user.ID.Value}")
            .AppendLine($"Profile: {user.ID.ToUserMention()}")
            .AppendLine($"Handle: {DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)}");

        var embed = new Embed(Title: $"{DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)} left",
            Description: builder.ToString(),
            Thumbnail: image,
            Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

        foreach (var channel in channels)
        {
            await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: ct);
        }

        return Result.FromSuccess();
    }
}
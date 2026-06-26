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

public class MemberJoinLeaveResponder(IMediator mediator, 
    IDiscordRestChannelAPI channelApi, 
    CoreEventQueue eventQueue,
    DiscordAvatarHelper discordAvatarHelper, 
    ThumbnailHelper thumbnailHelper) : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberRemove>
{
    public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (!gatewayEvent.User.HasValue)
            return Result.FromSuccess();

        var user = gatewayEvent.User.Value;

        var avatarUrl = discordAvatarHelper.GetAvatarUrl(user.ID.Value,
            user.Discriminator,
            user.Avatar?.Value,
            user.Avatar?.HasGif == true);

        await eventQueue.Queue(new AddUserRequest(user.ID.Value, user.Username, avatarUrl, null, gatewayEvent.JoinedAt));
        await eventQueue.Queue(new RaidCalculationRequest(gatewayEvent.GuildID.Value, new GuildUserDto(user.ID.Value, user.Username, avatarUrl, gatewayEvent.JoinedAt)));

        var channels = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.JoinLeaveLogs), ct);

        if (channels.Any())
        {
            var image = thumbnailHelper.GetAvatar(user);

            var builder = new StringBuilder()
                .AppendLine("**User Information**")
                .AppendLine($"ID: {user.ID.Value}")
                .AppendLine($"Profile: {user.ID.ToUserMention()}")
                .AppendLine($"Handle: {user.Username}")
                .AppendLine($"Created: {DiscordSnowflakeHelper.ToDateTimeOffset(user.ID.Value):yyy-MM-dd HH:mm:ss}");

            var embed = new Embed(Title: $"{user.Username} joined",
                Description: builder.ToString(),
                Thumbnail: image,
                Footer: new EmbedFooter($"{gatewayEvent.JoinedAt:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channels)
            {
                await channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: ct);
            }
        }

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        await eventQueue.Queue(new UpdateUserAsLeftRequest(gatewayEvent.User.ID.Value, DateTimeOffset.UtcNow));
        var channels = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.JoinLeaveLogs), ct);

        var image = thumbnailHelper.GetAvatar(gatewayEvent.User);

        var builder = new StringBuilder()
            .AppendLine("**User Information**")
            .AppendLine($"ID: {gatewayEvent.User.ID.Value}")
            .AppendLine($"Profile: {gatewayEvent.User.ID.ToUserMention()}")
            .AppendLine($"Handle: {gatewayEvent.User.Username}");

        var embed = new Embed(Title: $"{gatewayEvent.User.Username} left",
            Description: builder.ToString(),
            Thumbnail: image,
            Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"));

        foreach (var channel in channels)
        {
            await channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: ct);
        }

        return Result.FromSuccess();
    }
}
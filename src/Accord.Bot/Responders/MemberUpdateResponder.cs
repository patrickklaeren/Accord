using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.Users;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class MemberUpdateResponder(IMediator mediator, 
    IDiscordRestChannelAPI channelApi, 
    DiscordAvatarHelper discordAvatarHelper, 
    ThumbnailHelper thumbnailHelper) 
    : IResponder<IGuildMemberUpdate>
{
    public async Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken cancellationToken = new())
    {
        var user = gatewayEvent.User;

        var (hasDiff, messages) = await mediator.Send(
            new GetUserNameDiffRequest(
                user.ID.Value,
                user.Username,
                gatewayEvent.Nickname.HasValue ? gatewayEvent.Nickname.Value : null),
            cancellationToken);

        if (hasDiff && messages.Any())
        {
            await HandleUserDiff(user, messages, cancellationToken);
        }

        var avatarUrl = discordAvatarHelper.GetAvatarUrl(user.ID.Value,
            user.Discriminator,
            user.Avatar?.Value,
            user.Avatar?.HasGif == true);

        await mediator.Send(
            new UpdateUserRequest(user.ID.Value,
                user.Username,
                gatewayEvent.Nickname.HasValue ? gatewayEvent.Nickname.Value : null,
                gatewayEvent.CommunicationDisabledUntil.HasValue ? gatewayEvent.CommunicationDisabledUntil.Value : null,
                avatarUrl,
                gatewayEvent.JoinedAt),
            cancellationToken);

        return Result.FromSuccess();
    }

    private async Task HandleUserDiff(IUser user, IEnumerable<string> messages, CancellationToken cancellationToken)
    {
        var payload = string.Join(Environment.NewLine, messages);

        var channels =
            await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserUpdateLogs), cancellationToken);

        var image = thumbnailHelper.GetAvatar(user);

        var embed = new Embed(
            Title: $"{user.Username} updated",
            Description:
            $"{user.ID.ToUserMention()} ({user.ID.Value}){Environment.NewLine}{Environment.NewLine}{payload}",
            Thumbnail: image);

        foreach (var channel in channels)
        {
            await channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed },
                ct: cancellationToken);
        }
    }
}
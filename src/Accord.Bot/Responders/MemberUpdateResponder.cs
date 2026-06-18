using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.Users;
using Humanizer;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class MemberUpdateResponder(IMediator mediator, IDiscordRestChannelAPI channelApi, IDiscordRestAuditLogAPI auditLogApi, DiscordAvatarHelper discordAvatarHelper, ThumbnailHelper thumbnailHelper) : IResponder<IGuildMemberUpdate>
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

        if (gatewayEvent.CommunicationDisabledUntil is { HasValue: true, Value: { } until })
        {
            await HandleTimeOut(gatewayEvent, user, until, cancellationToken);
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

    private async Task HandleTimeOut(IGuildMemberUpdate gatewayEvent, IUser user, DateTimeOffset timedOutUntil, CancellationToken cancellationToken)
    {
        // Verify if the timeout value has changed vs what Accord knows to be true, if it has
        // changed then we know to send out a message to the alert channel. If it has not changed
        // then we won't bother
        var timeoutHasChanged = await mediator.Send(new HasTimeOutChangedRequest(user.ID.Value, timedOutUntil), cancellationToken);

        if (!timeoutHasChanged)
            return;

        var logRequest = await auditLogApi.GetGuildAuditLogAsync(gatewayEvent.GuildID, actionType: AuditLogEvent.MemberUpdate, ct: cancellationToken);

        var durationMessage = "has been timed out";
        var actor = "a moderator";
        var reason = "an unknown reason";

        if (logRequest.IsSuccess)
        {
            var rawUserId = user.ID.Value.ToString();

            var probableAudit = logRequest.Entity.AuditLogEntries
                .Where(x => x.TargetID == rawUserId)
                .Where(x => x.UserID != null)
                .Where(x => x.Changes.HasValue && x.Changes.Value.Any(a => a.Key == "communication_disabled_until"))
                .MaxBy(x => x.ID);

            if (probableAudit != null)
            {
                var timedOutFrom = probableAudit.ID.Timestamp;
                var durationOfTimeout = timedOutUntil - timedOutFrom;
                durationMessage = $"has been timed out for {durationOfTimeout.Humanize()}";
                actor = $"{DiscordFormatter.UserIdToMention(probableAudit.UserID!.Value.Value)}";
                reason = probableAudit.Reason.HasValue ? probableAudit.Reason.Value : "an unknown reason";
            }
        }

        var image = thumbnailHelper.GetAvatar(user);

        var timedOutUntilDiscordFormatted = DiscordFormatter.TimeToMarkdown(timedOutUntil);

        var embed = new Embed(
            Title: $"🤐 {user.Username} {durationMessage}",
            Description:
            $"{user.ID.ToUserMention()} ({user.ID.Value}){Environment.NewLine}{Environment.NewLine}Timed out until {timedOutUntilDiscordFormatted} by {actor} for {reason}",
            Thumbnail: image);

        var channels =
            await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.TimeOutLogs), cancellationToken);

        foreach (var channel in channels)
        {
            await channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed },
                ct: cancellationToken);
        }
    }
}
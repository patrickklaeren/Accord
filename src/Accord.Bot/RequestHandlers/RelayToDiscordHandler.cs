using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RelayToDiscordHandler(
    IMediator mediator,
    IDiscordRestChannelAPI channelApi,
    IDiscordRestUserAPI userApi,
    ThumbnailHelper thumbnailHelper)
    : INotificationHandler<RelayKickToDiscordRequest>,
        INotificationHandler<RelayBanToDiscordRequest>,
        INotificationHandler<RelayUnbanToDiscordRequest>,
        INotificationHandler<RelayWarningToDiscordRequest>,
        INotificationHandler<RelayNoteToDiscordRequest>
{
    public async Task Handle(RelayKickToDiscordRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserHistoryLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.TargetDiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;

        var title = $"👢 {targetUser.Entity.Username} kicked";
        var description = $"{DiscordFormatter.UserIdToMention(request.TargetDiscordUserId)} ({request.TargetDiscordUserId}) kicked by {DiscordFormatter.UserIdToMention(request.ActingDiscordUserId)} for {request.Reason}";
        await PostEmbedToChannels(targetUser.Entity, title, description, channelsToPostTo, cancellationToken);
    }

    public async Task Handle(RelayBanToDiscordRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserHistoryLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.TargetDiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;

        var title = $"🔨 {targetUser.Entity.Username} banned";
        var description = $"{DiscordFormatter.UserIdToMention(request.TargetDiscordUserId)} ({request.TargetDiscordUserId}) banned by {DiscordFormatter.UserIdToMention(request.ActingDiscordUserId)} for {request.Reason}";
        await PostEmbedToChannels(targetUser.Entity, title, description, channelsToPostTo, cancellationToken);
    }

    public async Task Handle(RelayUnbanToDiscordRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserHistoryLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.TargetDiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;

        var title = $"{targetUser.Entity.Username} unbanned";
        var description = $"{DiscordFormatter.UserIdToMention(request.TargetDiscordUserId)} ({request.TargetDiscordUserId}) unbanned by {DiscordFormatter.UserIdToMention(request.ActingDiscordUserId)} for {request.Reason}";
        await PostEmbedToChannels(targetUser.Entity, title, description, channelsToPostTo, cancellationToken);
    }

    public async Task Handle(RelayWarningToDiscordRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserHistoryLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.TargetDiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;
        
        var title = $"⚠ ️{targetUser.Entity.Username} warned";
        var description = $"{DiscordFormatter.UserIdToMention(request.TargetDiscordUserId)} ({request.TargetDiscordUserId}) warned by {DiscordFormatter.UserIdToMention(request.ActingDiscordUserId)} for {request.Reason}";
        await PostEmbedToChannels(targetUser.Entity, title, description, channelsToPostTo, cancellationToken);
    }

    public async Task Handle(RelayNoteToDiscordRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserHistoryLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var targetUser = await userApi.GetUserAsync(new Snowflake(request.TargetDiscordUserId),
            cancellationToken);

        if (!targetUser.IsSuccess)
            return;

        var title = $"️📝 {targetUser.Entity.Username} remarked";
        var description = $"{DiscordFormatter.UserIdToMention(request.TargetDiscordUserId)} ({request.TargetDiscordUserId}) remarked by {DiscordFormatter.UserIdToMention(request.ActingDiscordUserId)} for {request.Reason}";
        await PostEmbedToChannels(targetUser.Entity, title, description, channelsToPostTo, cancellationToken);
    }

    private async Task PostEmbedToChannels(IUser user, 
        string embedTitle,
        string embedDescription,
        IReadOnlyCollection<ulong> channelIds, 
        CancellationToken cancellationToken)
    {
        var targetGuildMemberAvatar = thumbnailHelper.GetAvatar(user);
        
        var embed = new Embed(
            Title: embedTitle,
            Thumbnail: targetGuildMemberAvatar,
            Description: embedDescription,
            Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"));
        
        foreach (var channelId in channelIds)
        {
            await channelApi.CreateMessageAsync(new Snowflake(channelId), embeds: new[] { embed }, ct: cancellationToken);   
        }
    }
}
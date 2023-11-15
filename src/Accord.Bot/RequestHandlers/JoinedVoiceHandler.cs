using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.VoiceSessions;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

[AutoConstructor]
public partial class JoinedVoiceHandler : IRequestHandler<JoinedVoiceRequest>
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IMediator _mediator;
    private readonly ThumbnailHelper _thumbnailHelper;

    public async Task Handle(JoinedVoiceRequest request, CancellationToken cancellationToken)
    {
        var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.VoiceLogs), cancellationToken);

        if (!channels.Any())
            return;

        var guildMember = await _guildApi.GetGuildMemberAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.DiscordUserId), cancellationToken);

        if (!guildMember.IsSuccess || !guildMember.Entity.User.HasValue)
            return;

        var user = guildMember.Entity.User.Value!;

        var avatar = _thumbnailHelper.GetAvatar(user);

        var embed = new Embed(Title: $"📢 {DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)} joined voice",
            Description: $"{user.ID.ToUserMention()} ({user.ID.Value}) joined {DiscordFormatter.ChannelIdToMention(request.DiscordChannelId)}",
            Footer: new EmbedFooter($"Session ID: {request.DiscordSessionId}"),
            Thumbnail: avatar);

        foreach (var channel in channels)
        {
            await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
        }
    }
}
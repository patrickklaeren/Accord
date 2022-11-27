using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.NamePatterns;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class NamePatternAlertHandler : AsyncRequestHandler<NamePatternAlertRequest>
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IMediator _mediator;
    private readonly ThumbnailHelper _thumbnailHelper;

    public NamePatternAlertHandler(IDiscordRestChannelAPI channelApi, 
        IMediator mediator, IDiscordRestGuildAPI guildApi, 
        ThumbnailHelper thumbnailHelper)
    {
        _channelApi = channelApi;
        _mediator = mediator;
        _guildApi = guildApi;
        _thumbnailHelper = thumbnailHelper;
    }

    protected override async Task Handle(NamePatternAlertRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.BanKickLogs), cancellationToken);

        if (!channelsToPostTo.Any())
            return;

        var guildUser = await _guildApi.GetGuildMemberAsync(new Snowflake(request.DiscordGuildId),
            new Snowflake(request.User.Id),
            cancellationToken);

        if (!guildUser.IsSuccess || guildUser.Entity is null || !guildUser.Entity.User.HasValue)
            return;

        var user = guildUser.Entity.User.Value;

        var image = _thumbnailHelper.GetAvatar(user!);

        var embed = new Embed(Title: "🚨 User name matches pattern",
            Description: $"{user.ID.ToUserMention()} ({user.ID.Value})",
            Thumbnail: image,
            Footer: new EmbedFooter($"Matched on {request.MatchedOnPattern}"));

        foreach (var channel in channelsToPostTo)
        {
            await _channelApi.CreateMessageAsync(new Snowflake(channel), content: string.Empty, embeds: new[] { embed }, ct: cancellationToken);
        }
    }
}
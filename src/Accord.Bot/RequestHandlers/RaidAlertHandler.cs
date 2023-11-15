using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Raid;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

[AutoConstructor]
public partial class RaidAlertHandler : IRequestHandler<RaidAlertRequest>
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IMediator _mediator;

    public async Task Handle(RaidAlertRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.RaidLogs), cancellationToken);

        var embed = new Embed(Title: "🚨 Raid detected",
            Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

        foreach (var channel in channelsToPostTo)
        {
            await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
        }
    }
}
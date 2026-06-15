using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.UserMessages;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class CryptoScamAlertHandler(IDiscordRestChannelAPI discordRestChannelApi, IMediator mediator) : IRequestHandler<CryptoScamAlertRequest>
{
    public async Task Handle(CryptoScamAlertRequest request, CancellationToken cancellationToken)
    {
        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.RaidLogs), cancellationToken);

        var handle = DiscordFormatter.UserIdToMention(request.DiscordUserId);
        var embed = new Embed(Title: $"🤖 Possible scam posted by {handle}",
            Description: request.FileUrl,
            Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"));

        foreach (var channel in channelsToPostTo)
        {
            await discordRestChannelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
        }
    }
}
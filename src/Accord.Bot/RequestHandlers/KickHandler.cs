using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.Moderation;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Discord.Rest.API;

namespace Accord.Bot.RequestHandlers;

[AutoConstructor]
public partial class KickHandler : IRequestHandler<KickRequest>
{
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IMediator _mediator;

    public async Task Handle(KickRequest request, CancellationToken cancellationToken)
    {
        using (_ = ((DiscordRestGuildAPI)_guildApi).WithCustomization(r => r.AddHeader("X-Audit-Log-Reason", request.Reason)))
        {
            await _guildApi.RemoveGuildMemberAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.User.Id), ct: cancellationToken);
        }

        var channelsToPostTo = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.BanKickLogs), cancellationToken);

        if (channelsToPostTo.Any())
        {
            var embed = new Embed(Title: $"👢 Kicked {DiscordHandleHelper.BuildHandle(request.User.Username, request.User.Discriminator)}",
                Description: $"{DiscordFormatter.UserIdToMention(request.User.Id)} ({request.User.Id}) kicked for reason {request.Reason}",
                Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channelsToPostTo)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Moderation;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Discord.Rest.API;

namespace Accord.Bot.RequestHandlers;

public class KickHandler(IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, IMediator mediator) : IRequestHandler<KickRequest>
{

    public async Task Handle(KickRequest request, CancellationToken cancellationToken)
    {
        using (_ = ((DiscordRestGuildAPI)guildApi).WithCustomization(r => r.AddHeader("X-Audit-Log-Reason", request.Reason)))
        {
            await guildApi.RemoveGuildMemberAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.DiscordUserId), ct: cancellationToken);
        }

        var channelsToPostTo = await mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.BanKickLogs), cancellationToken);

        if (channelsToPostTo.Any())
        {
            var embed = new Embed(Title: $"👢 Kicked {request.DiscordUsername}",
                Description: $"{DiscordFormatter.UserIdToMention(request.DiscordUserId)} ({request.DiscordUserId}) kicked for reason {request.Reason}",
                Footer: new EmbedFooter($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channelsToPostTo)
            {
                await channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
            }
        }
    }
}
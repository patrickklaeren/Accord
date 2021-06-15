using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class RaidAlertRequestHandler : AsyncRequestHandler<RaidAlertRequest>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly ChannelFlagService _channelFlagService;
        private static readonly Snowflake PatrickSnowflake = new(104975006542372864);

        public RaidAlertRequestHandler(IDiscordRestChannelAPI channelApi, ChannelFlagService channelFlagService)
        {
            _channelApi = channelApi;
            _channelFlagService = channelFlagService;
        }

        protected override async Task Handle(RaidAlertRequest request, CancellationToken cancellationToken)
        {
            if (!request.IsRaidDetected)
                return;

            if (request.IsRaidDetected && request.IsInExistingRaidMode)
                return;

            if (request.IsRaidDetected && !request.IsInExistingRaidMode)
            {
                var channelsToPostTo = await _channelFlagService.GetChannelsWithFlag(ChannelFlagType.RaidLogs);

                var embed = new Embed(Title: "🚨 Raid detected",
                    Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

                foreach (var channel in channelsToPostTo)
                {
                    await _channelApi.CreateMessageAsync(new Snowflake(channel), content: PatrickSnowflake.ToUserMention(), embed: embed, ct: cancellationToken);

                    // Artificial delay because Discord
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                }
            }
        }
    }
}

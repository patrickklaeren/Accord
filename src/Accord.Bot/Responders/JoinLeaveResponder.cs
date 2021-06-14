using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class JoinLeaveResponder : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberRemove>
    {
        private readonly ChannelFlagService _channelFlagService;
        private readonly IDiscordRestChannelAPI _channelApi;

        public JoinLeaveResponder(ChannelFlagService channelFlagService, IDiscordRestChannelAPI channelApi)
        {
            _channelFlagService = channelFlagService;
            _channelApi = channelApi;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (!gatewayEvent.User.HasValue)
                return Result.FromSuccess();

            var channels = await _channelFlagService.GetChannelsWithFlag(ChannelFlagType.JoinLeaveLogs);

            var embed = new Embed(Title: "User Joined", Description: $"{gatewayEvent.User.Value.ID.ToUserMention()} ({gatewayEvent.User.Value.ID.Value})", Footer: new EmbedFooter($"{gatewayEvent.JoinedAt:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channels)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: ct);

                // Artificial delay because Discord
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var channels = await _channelFlagService.GetChannelsWithFlag(ChannelFlagType.JoinLeaveLogs);

            var embed = new Embed(Title: "User left", Description: $"{gatewayEvent.User.ID.ToUserMention()} ({gatewayEvent.User.ID.Value})", Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channels)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: ct);

                // Artificial delay because Discord
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }

            return Result.FromSuccess();
        }
    }
}

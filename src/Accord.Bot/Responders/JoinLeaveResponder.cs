using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Accord.Domain.Model;
using Accord.Services;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
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
        private readonly DiscordConfiguration _discordConfiguration;

        public JoinLeaveResponder(ChannelFlagService channelFlagService, IDiscordRestChannelAPI channelApi, IOptions<DiscordConfiguration> discordConfiguration)
        {
            _channelFlagService = channelFlagService;
            _channelApi = channelApi;
            _discordConfiguration = discordConfiguration.Value;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (!gatewayEvent.User.HasValue)
                return Result.FromSuccess();

            var user = gatewayEvent.User.Value;

            var channels = await _channelFlagService.GetChannelsWithFlag(ChannelFlagType.JoinLeaveLogs);

            var image = GetAvatar(user);

            var embed = new Embed(Title: "User Joined",
                Description: $"{user.ID.ToUserMention()} ({user.ID.Value})",
                Thumbnail: image,
                Footer: new EmbedFooter($"{gatewayEvent.JoinedAt:yyyy-MM-dd HH:mm:ss}"));

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

            var image = GetAvatar(gatewayEvent.User);

            var embed = new Embed(Title: "User left", 
                Description: $"{gatewayEvent.User.ID.ToUserMention()} ({gatewayEvent.User.ID.Value})", 
                Thumbnail: image,
                Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channels)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: ct);

                // Artificial delay because Discord
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }

            return Result.FromSuccess();
        }

        private EmbedThumbnail? GetAvatar(IUser user)
        {
            if (user.Avatar is null)
            {
                return null;
            }

            var extension = "png";

            if (user.Avatar.HasGif)
            {
                extension = "gif";
            }

            var url = $"{_discordConfiguration.CdnBaseUrl}/avatars/{user.ID.Value}/{user.Avatar.Value}.{extension}";

            return new EmbedThumbnail(url);

        }
    }
}

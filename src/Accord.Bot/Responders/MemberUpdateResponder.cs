using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.Users;
using MediatR;
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
    public class MemberUpdateResponder : IResponder<IGuildMemberUpdate>
    {
        private readonly IMediator _mediator;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly DiscordConfiguration _discordConfiguration;

        public MemberUpdateResponder(IMediator mediator, IOptions<DiscordConfiguration> discordConfiguration, IDiscordRestChannelAPI channelApi)
        {
            _mediator = mediator;
            _channelApi = channelApi;
            _discordConfiguration = discordConfiguration.Value;
        }

        public async Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var user = gatewayEvent.User;

            var diff = await _mediator.Send(new GetDiffForUserRequest(user.ID.Value, user.Username,
                user.Discriminator.ToPaddedDiscriminator(), gatewayEvent.Nickname.Value));

            if (diff.HasDiff)
            {

                var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserUpdateLogs), ct);

                var image = GetAvatar(user);

                var payload = string.Join(Environment.NewLine, diff.Messages);

                var embed = new Embed(Title: "User Update",
                    Description: $"{user.ID.ToUserMention()} ({user.ID.Value}){Environment.NewLine}{Environment.NewLine}{payload}",
                    Thumbnail: image);

                foreach (var channel in channels)
                {
                    await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: ct);

                    // Artificial delay because Discord
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }

            await _mediator.Send(new UpdateUserRequest(user.ID.Value, user.Username,
                user.Discriminator.ToPaddedDiscriminator(), gatewayEvent.Nickname.Value, gatewayEvent.JoinedAt), ct);

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

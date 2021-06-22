using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.Users;
using MediatR;
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
        private readonly DiscordAvatarHelper _discordAvatarHelper;

        public MemberUpdateResponder(IMediator mediator, 
            IDiscordRestChannelAPI channelApi, DiscordAvatarHelper discordAvatarHelper)
        {
            _mediator = mediator;
            _channelApi = channelApi;
            _discordAvatarHelper = discordAvatarHelper;
        }

        public async Task<Result> RespondAsync(IGuildMemberUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var user = gatewayEvent.User;

            var diff = await _mediator.Send(new GetDiffForUserRequest(user.ID.Value, user.Username,
                user.Discriminator.ToPaddedDiscriminator(), gatewayEvent.Nickname.HasValue ? gatewayEvent.Nickname.Value : null));

            if (diff.HasDiff)
            {
                var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.UserUpdateLogs), ct);

                var image = _discordAvatarHelper.GetAvatar(user);

                var payload = string.Join(Environment.NewLine, diff.Messages);

                if (!string.IsNullOrWhiteSpace(payload))
                {
                    var embed = new Embed(Title: $"{DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)} updated",
                        Description: $"{user.ID.ToUserMention()} ({user.ID.Value}){Environment.NewLine}{Environment.NewLine}{payload}",
                        Thumbnail: image);

                    foreach (var channel in channels)
                    {
                        await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: ct);
                    }
                }
            }

            await _mediator.Send(new UpdateUserRequest(gatewayEvent.GuildID.Value, user.ID.Value, user.Username,
                user.Discriminator.ToPaddedDiscriminator(), gatewayEvent.Nickname.HasValue ? gatewayEvent.Nickname.Value : null, gatewayEvent.JoinedAt), ct);

            return Result.FromSuccess();
        }
    }
}

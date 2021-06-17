using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.ChannelFlags;
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
    public class MemberJoinLeaveResponder : IResponder<IGuildMemberAdd>, IResponder<IGuildMemberRemove>
    {
        private readonly IMediator _mediator;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IEventQueue _eventQueue;
        private readonly DiscordAvatarHelper _discordAvatarHelper;

        public MemberJoinLeaveResponder(IMediator mediator, IDiscordRestChannelAPI channelApi, IEventQueue eventQueue, DiscordAvatarHelper discordAvatarHelper)
        {
            _mediator = mediator;
            _channelApi = channelApi;
            _eventQueue = eventQueue;
            _discordAvatarHelper = discordAvatarHelper;
        }

        public async Task<Result> RespondAsync(IGuildMemberAdd gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (!gatewayEvent.User.HasValue)
                return Result.FromSuccess();

            var user = gatewayEvent.User.Value;

            await _eventQueue.Queue(new UserJoinedEvent(gatewayEvent.GuildID.Value, user.ID.Value, gatewayEvent.JoinedAt, user.Username, user.Discriminator.ToPaddedDiscriminator(), null));

            var queueTask = _eventQueue.Queue(new RaidCalculationEvent(user.ID.Value, gatewayEvent.JoinedAt));

            var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.JoinLeaveLogs), ct);

            var image = _discordAvatarHelper.GetAvatar(user);

            var embed = new Embed(Title: "User joined",
                Description: $"{user.ID.ToUserMention()} ({user.ID.Value})",
                Thumbnail: image,
                Footer: new EmbedFooter($"{gatewayEvent.JoinedAt:yyyy-MM-dd HH:mm:ss}"));

            foreach (var channel in channels)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embed: embed, ct: ct);

                // Artificial delay because Discord
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }

            await queueTask;

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.JoinLeaveLogs), ct);

            var image = _discordAvatarHelper.GetAvatar(gatewayEvent.User);

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
    }
}

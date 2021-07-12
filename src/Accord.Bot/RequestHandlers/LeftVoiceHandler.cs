using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Helpers;
using Accord.Services.VoiceSessions;
using Humanizer;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class LeftVoiceHandler : AsyncRequestHandler<LeftVoiceRequest>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IMediator _mediator;
        private readonly DiscordAvatarHelper _discordAvatarHelper;

        public LeftVoiceHandler(IDiscordRestChannelAPI channelApi,
            IMediator mediator, IDiscordRestGuildAPI guildApi,
            DiscordAvatarHelper discordAvatarHelper)
        {
            _channelApi = channelApi;
            _mediator = mediator;
            _guildApi = guildApi;
            _discordAvatarHelper = discordAvatarHelper;
        }

        protected override async Task Handle(LeftVoiceRequest request, CancellationToken cancellationToken)
        {
            var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.VoiceLogs), cancellationToken);

            if (!channels.Any())
                return;

            var guildMember = await _guildApi.GetGuildMemberAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.DiscordUserId), cancellationToken);

            if (!guildMember.IsSuccess || !guildMember.Entity.User.HasValue)
                return;

            var user = guildMember.Entity.User.Value!;

            var avatar = _discordAvatarHelper.GetAvatar(user);

            var span = (request.DisconnectedDateTime - request.ConnectedDateTime).Humanize();

            var embed = new Embed(Title: $"📢 {DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)} left voice",
                Description: $"{user.ID.ToUserMention()} ({user.ID.Value}) left {DiscordFormatter.ChannelIdToMention(request.DiscordChannelId)} after {span}",
                Footer: new EmbedFooter($"Session ID: {request.DiscordSessionId}"),
                Thumbnail: avatar);

            foreach (var channel in channels)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
            }
        }
    }
}

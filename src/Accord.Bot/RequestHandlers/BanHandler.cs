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
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class BanHandler : AsyncRequestHandler<BanRequest>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IMediator _mediator;

        public BanHandler(IDiscordRestChannelAPI channelApi, IMediator mediator, 
            IDiscordRestGuildAPI guildApi)
        {
            _channelApi = channelApi;
            _mediator = mediator;
            _guildApi = guildApi;
        }

        protected override async Task Handle(BanRequest request, CancellationToken cancellationToken)
        {
            await _guildApi.CreateGuildBanAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.User.Id), reason: request.Reason, ct: cancellationToken);

            var channelsToPostTo = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.BanKickLogs), cancellationToken);

            if (channelsToPostTo.Any())
            {
                var embed = new Embed(Title: $"🔨 Banned {DiscordHandleHelper.BuildHandle(request.User.Username, request.User.Discriminator)}",
                    Description: $"{DiscordFormatter.UserIdToMention(request.User.Id)} ({request.User.Id}) banned for reason {request.Reason}",
                    Footer: new EmbedFooter($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"));

                foreach (var channel in channelsToPostTo)
                {
                    await _channelApi.CreateMessageAsync(new Snowflake(channel), embeds: new[] { embed }, ct: cancellationToken);
                }
            }
        }
    }
}
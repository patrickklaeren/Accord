using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.NamePatterns;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class NamePatternKickHandler : AsyncRequestHandler<NamePatternKickRequest>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IMediator _mediator;
        private readonly DiscordAvatarHelper _discordAvatarHelper;
        private readonly ILogger<NamePatternKickHandler> _logger;

        public NamePatternKickHandler(IDiscordRestChannelAPI channelApi,
            IMediator mediator, IDiscordRestGuildAPI guildApi,
            DiscordAvatarHelper discordAvatarHelper,
            ILogger<NamePatternKickHandler> logger)
        {
            _channelApi = channelApi;
            _mediator = mediator;
            _guildApi = guildApi;
            _discordAvatarHelper = discordAvatarHelper;
            _logger = logger;
        }

        protected override async Task Handle(NamePatternKickRequest request, CancellationToken cancellationToken)
        {
            var channelsToPostTo = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.BanKickLogs), cancellationToken);

            if (!channelsToPostTo.Any())
                return;

            var guildUser = await _guildApi.GetGuildMemberAsync(new Snowflake(request.DiscordGuildId),
                new Snowflake(request.DiscordUserId),
                cancellationToken);

            if (!guildUser.IsSuccess || guildUser.Entity is null || !guildUser.Entity.User.HasValue)
                return;

            var user = guildUser.Entity.User.Value;

            try
            {
                await _guildApi.RemoveGuildMemberAsync(new Snowflake(request.DiscordGuildId), new Snowflake(request.DiscordUserId), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed removing member {DiscordUserId} from guild {DiscordGuildId} for matching pattern", 
                    request.DiscordUserId, request.DiscordGuildId);
            }

            var image = _discordAvatarHelper.GetAvatar(user!);

            var embed = new Embed(Title: "🚨 User name matches pattern",
                Description: $"{user.ID.ToUserMention()} ({user.ID.Value})",
                Thumbnail: image,
                Footer: new EmbedFooter($"Matched on {request.MatchedOnPattern}"));

            foreach (var channel in channelsToPostTo)
            {
                await _channelApi.CreateMessageAsync(new Snowflake(channel), content: Constants.StaffSnowflake.ToRoleMention(), embed: embed, ct: cancellationToken);
                // Artificial delay because Discord
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}

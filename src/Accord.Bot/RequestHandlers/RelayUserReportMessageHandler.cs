using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class RelayUserReportMessageHandler : AsyncRequestHandler<RelayUserReportMessageRequest>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly DiscordCache _discordCache;

        public RelayUserReportMessageHandler(IDiscordRestChannelAPI channelApi, DiscordCache discordCache)
        {
            _channelApi = channelApi;
            _discordCache = discordCache;
        }

        protected override async Task Handle(RelayUserReportMessageRequest request, CancellationToken cancellationToken)
        {
            var member = await _discordCache.GetGuildMember(request.DiscordGuildId, request.AuthorDiscordUserId);

            if (!member.IsSuccess || member.Entity is null || !member.Entity.User.HasValue)
                return;

            var user = member.Entity.User.Value;

            var embed = new Embed()
            {
                Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)),
                Description = request.Content,
                Footer = new EmbedFooter(request.SentDateTime.ToString("O")),
            };

            await _channelApi.CreateMessageAsync(new Snowflake(request.ToDiscordChannelId), embed: embed, ct: cancellationToken);
        }
    }
}
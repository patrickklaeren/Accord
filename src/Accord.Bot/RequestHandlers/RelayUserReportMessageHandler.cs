using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
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

            EmbedImage? image = null;

            var topImage = request.DiscordAttachments.FirstOrDefault(x => x.ContentType?.StartsWith("image") == true);

            if (topImage is not null)
            {
                image = new EmbedImage(topImage.Url);
            }

            var otherAttachments = request.DiscordAttachments
                .Where(x => x.ContentType?.StartsWith("image") == false)
                .Select((file, index) => new EmbedField($"{Path.GetFileName(file.Url)}", DiscordFormatter.ToFormattedUrl("Download", file.Url)))
                .ToList();

            var embed = new Embed()
            {
                Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)),
                Image = image,
                Description = request.Content,
                Fields = otherAttachments,
                Footer = new EmbedFooter(request.SentDateTime.ToString("yyyy-MM-dd HH:mm:ss")),
            };

            await _channelApi.CreateMessageAsync(new Snowflake(request.ToDiscordChannelId), embeds: new[] { embed }, ct: cancellationToken);
        }
    }
}
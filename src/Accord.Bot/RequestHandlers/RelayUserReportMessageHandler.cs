using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Helpers;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Accord.Bot.RequestHandlers
{
    public class RelayUserReportMessageHandler : IRequestHandler<RelayUserReportMessageRequest, ServiceResponse>
    {
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly DiscordAvatarHelper _discordAvatarHelper;
        private readonly DiscordCache _discordCache;
        private readonly IMediator _mediator;

        public RelayUserReportMessageHandler(
            DiscordCache discordCache,
            IDiscordRestWebhookAPI webhookApi,
            DiscordAvatarHelper discordAvatarHelper,
            IMediator mediator)
        {
            _discordCache = discordCache;
            _webhookApi = webhookApi;
            _discordAvatarHelper = discordAvatarHelper;
            _mediator = mediator;
        }

        public async Task<ServiceResponse> Handle(RelayUserReportMessageRequest request, CancellationToken cancellationToken)
        {
            var member = await _discordCache.GetGuildMember(request.DiscordGuildId, request.AuthorDiscordUserId);

            if (!member.IsSuccess || member.Entity is null || !member.Entity.User.HasValue)
                return ServiceResponse.Fail("Member is invalid");

            var user = member.Entity.User.Value;

            EmbedImage? image = null;

            var topImage = request.DiscordAttachments.FirstOrDefault(x => x.ContentType?.StartsWith("image") == true);

            if (topImage is not null)
            {
                image = new EmbedImage(topImage.Url);
            }

            var avatarUrl = _discordAvatarHelper.GetAvatarUrl(member.Entity.User.Value);
            var username = member.Entity.Nickname.Value ?? user.Username;

            var otherAttachments = request.DiscordAttachments
                .Where(x => x.ContentType?.StartsWith("image") == false)
                .Select((file, index) => new EmbedField(
                    $"{Path.GetFileName(file.Url)}",
                    DiscordFormatter.ToFormattedUrl("Download", file.Url)))
                .ToList();

            Embed? embed = null;
            if (image != null || otherAttachments is {Count: > 0})
            {
                embed = new Embed
                {
                    Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator), IconUrl: avatarUrl!),
                    Image = image!,
                    Fields = otherAttachments,
                    Footer = new EmbedFooter(request.SentDateTime.ToString("yyyy-MM-dd HH:mm:ss")),
                };
            }

            var proxiedMessage = await _webhookApi.ExecuteWebhookAsync(
                new Snowflake(request.DiscordProxyWebhookId),
                request.DiscordProxyWebhookToken,
                shouldWait: true,
                content: request.Content,
                embeds: embed != null ? new List<IEmbed> {embed} : null!,
                avatarUrl: avatarUrl!,
                username: username,
                ct: cancellationToken);

            return await _mediator.Send(new AttachProxiedMessageIdToMessageRequest(request.OriginalDiscordMessageId, proxiedMessage.Entity!.ID.Value), cancellationToken);
        }
    }
}
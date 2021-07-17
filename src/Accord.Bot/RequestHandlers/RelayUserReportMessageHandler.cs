using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using Remora.Results;

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

            var textSnippet = request.DiscordAttachments.FirstOrDefault(x => x.ContentType?.StartsWith("text") == true);

            Optional<FileData> fileData = default(Optional<FileData>);
            if (textSnippet is not null)
            {
                var stream = await new HttpClient().GetStreamAsync(textSnippet.Url, cancellationToken);
                var fileNameExtension = Path.GetExtension(textSnippet.FileName);
                fileData = new FileData($"{Guid.NewGuid()}{(String.IsNullOrEmpty(fileNameExtension) ? "" : $".{fileNameExtension}")}", stream);
            }

            var avatarUrl = _discordAvatarHelper.GetAvatarUrl(member.Entity.User.Value);
            var username = member.Entity.Nickname.Value ?? user.Username;

            var otherAttachments = request.DiscordAttachments
                .Where(x => x.ContentType != null && !x.ContentType.StartsWith("image") && !x.ContentType.StartsWith("text") || x.ContentType == null)
                .Select((file, index) => new EmbedField(
                    $"{Path.GetFileName(file.Url)}",
                    DiscordFormatter.ToFormattedUrl("Download", file.Url)))
                .ToList();

            List<Embed> embeds = new();
            if (image != null || otherAttachments is {Count: > 0})
            {
                embeds.Add(new Embed
                {
                    Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator), IconUrl: avatarUrl!),
                    Image = image!,
                    Fields = otherAttachments,
                    Footer = new EmbedFooter(request.SentDateTime.ToString("yyyy-MM-dd HH:mm:ss")),
                });
            }

            List<ServiceResponse> serviceResponses = new();
            int messageCount = (int)Math.Min(1, Math.Ceiling(request.Content.Length / 2000f) + 1);

            if (messageCount == 1 && !embeds.Any() && fileData == default && String.IsNullOrEmpty(request.Content?.Trim()))
            {
                embeds.Add(new Embed
                {
                    Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator), IconUrl: avatarUrl!),
                    Description = "User sent a message that couldn't be relayed. Probably a sticker"
                });
            }

            if (request.DiscordMessageReferenceId != null)
            {
                var originalMessage = await _mediator.Send(new GetUserReportMessageRequest(request.DiscordMessageReferenceId.Value), cancellationToken);
                var originalAuthor = await _discordCache.GetGuildMember(request.DiscordGuildId, originalMessage.AuthorUserId);
                var originalAuthorUser = originalAuthor.Entity!.User.Value;
                var originalAuthorAvatarUrl = _discordAvatarHelper.GetAvatarUrl(originalAuthorUser);
                embeds.Add(new Embed
                {
                    Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(originalAuthor.Entity.User.Value.Username, originalAuthor.Entity.User.Value.Discriminator),
                        IconUrl: originalAuthorAvatarUrl!),
                    Title = !string.IsNullOrEmpty(originalMessage.Content) ? "Replying to the following message" : "Replied to a sticker which cannot be relayed",
                    Description = originalMessage.Content
                });
            }

            var allowedMentions = new AllowedMentions(Users: new List<Snowflake>(), Roles: new List<Snowflake>());

            for (int i = 0; i < messageCount; i++)
            {
                Result<IMessage?> proxiedMessage;

                var contentPart = request.Content?.Substring(i * 2000, Math.Min(2000, request.Content.Length - i * 2000));

                proxiedMessage = await _webhookApi.ExecuteWebhookAsync(
                    new Snowflake(request.DiscordProxyWebhookId),
                    request.DiscordProxyWebhookToken,
                    shouldWait: true,
                    content: contentPart ?? string.Empty,
                    file: i == messageCount - 1 ? fileData : default(Optional<FileData>),
                    embeds: i == messageCount - 1 ? embeds : default(Optional<IReadOnlyList<IEmbed>>),
                    avatarUrl: avatarUrl!,
                    allowedMentions: allowedMentions,
                    username: username,
                    ct: cancellationToken);

                var response = await _mediator.Send(new AttachProxiedMessageIdToMessageRequest(request.OriginalDiscordMessageId, proxiedMessage.Entity!.ID.Value),
                    cancellationToken);

                serviceResponses.Add(response);
            }

            return serviceResponses.All(x => x.Success) ? ServiceResponse.Ok() : serviceResponses.First(x => x.Failure);
        }
    }
}
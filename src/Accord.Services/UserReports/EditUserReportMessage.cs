using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;

namespace Accord.Services.UserReports
{
    public sealed record EditUserReportMessageRequest(
            ulong DiscordMessageId,
            ulong DiscordChannelId,
            UserReportChannelType DiscordChannelType,
            string Content,
            List<DiscordAttachmentDto> Attachments)
        : IRequest<ServiceResponse>;

    public sealed record EditUserReportDiscordMessageRequest(
            ulong DiscordProxyWebhookId,
            string DiscordProxyWebhookToken,
            ulong DiscordProxiedMessageId,
            string Content,
            List<DiscordAttachmentDto> Attachments)
        : IRequest<ServiceResponse>;

    public class EditUserReportMessageHandler : IRequestHandler<EditUserReportMessageRequest, ServiceResponse>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public EditUserReportMessageHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }
        
        public async Task<ServiceResponse> Handle(EditUserReportMessageRequest request, CancellationToken cancellationToken)
        {
            if (request.DiscordChannelType == UserReportChannelType.None)
                return ServiceResponse.Fail("DiscordChannelType was none");
            
            var userReportData = await _mediator.Send(new GetUserReportByChannelRequest(request.DiscordChannelId), cancellationToken);

            if (userReportData == null)
            {
                return ServiceResponse.Fail("Couldn't retrieve report's information");
            }

            ulong webhookId;
            string webhookToken;
            if (request.DiscordChannelType == UserReportChannelType.Inbox)
            {
                webhookId = userReportData.OutboxDiscordMessageProxyWebhookId;
                webhookToken = userReportData.OutboxDiscordMessageProxyWebhookToken;
            }
            else if (request.DiscordChannelType == UserReportChannelType.Outbox)
            {
                webhookId = userReportData.InboxDiscordMessageProxyWebhookId;
                webhookToken = userReportData.InboxDiscordMessageProxyWebhookToken;
            }
            else
                throw new NotSupportedException($"Discord Channel Type {request.DiscordChannelType} is not supported");
            
            var userMessage = await _mediator.Send(new GetUserReportMessageRequest(request.DiscordMessageId), cancellationToken);

            if (userMessage == null)
            {
                return ServiceResponse.Fail("Couldn't retrieve message's information");
            }

            userMessage.Content = request.Content;

            _db.Update(userMessage);
            await _db.SaveChangesAsync(cancellationToken);

            //todo log update on an audit log? maybe trigger?
            await _mediator.Send(new InvalidateGetUserReportMessageRequest(userMessage.Id), cancellationToken);
            
            return await _mediator.Send(
                new EditUserReportDiscordMessageRequest(
                    webhookId,
                  webhookToken,
                    userMessage.DiscordProxyMessageId,
                    request.Content,
                    request.Attachments),
                cancellationToken);
        }
    }
}
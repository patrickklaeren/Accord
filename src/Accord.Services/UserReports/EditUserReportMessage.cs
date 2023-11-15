using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;

namespace Accord.Services.UserReports;

public sealed record EditUserReportMessageRequest(
        ulong DiscordMessageId,
        ulong DiscordChannelId,
        UserReportChannelType DiscordChannelType,
        string Content,
        List<DiscordAttachmentDto> Attachments)
    : IRequest;

public sealed record EditUserReportDiscordMessageRequest(
        ulong DiscordProxyWebhookId,
        string DiscordProxyWebhookToken,
        ulong DiscordProxiedMessageId,
        string Content,
        List<DiscordAttachmentDto> Attachments)
    : IRequest;

[AutoConstructor]
public partial class EditUserReportMessageHandler : IRequestHandler<EditUserReportMessageRequest>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public async Task Handle(EditUserReportMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.DiscordChannelType == UserReportChannelType.None)
        {
            return;
        }

        var userReportData = await _mediator.Send(new GetUserReportByChannelRequest(request.DiscordChannelId), cancellationToken);

        if (userReportData == null)
        {
            return;
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
            return;
        }

        userMessage.Content = request.Content;

        _db.Update(userMessage);
        await _db.SaveChangesAsync(cancellationToken);

        //todo log update on an audit log? maybe trigger?
        await _mediator.Send(new InvalidateGetUserReportMessageRequest(userMessage.Id), cancellationToken);

        await _mediator.Send(
            new EditUserReportDiscordMessageRequest(
                webhookId,
                webhookToken,
                userMessage.DiscordProxyMessageId,
                request.Content,
                request.Attachments),
            cancellationToken);
    }
}
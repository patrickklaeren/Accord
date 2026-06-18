using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model.UserReports;
using MediatR;

namespace Accord.Services.UserReports;

public sealed record DeleteUserReportDiscordMessageRequest(
        ulong DiscordProxyWebhookId,
        string DiscordProxyWebhookToken,
        ulong DiscordProxiedMessageId)
    : IRequest;

public sealed record DeleteUserReportMessageRequest(
        ulong DiscordMessageId,
        ulong DiscordChannelId,
        UserReportChannelType DiscordChannelType)
    : IRequest;

public class DeleteUserReportMessageHandler(AccordContext db, IMediator mediator) : IRequestHandler<DeleteUserReportMessageRequest>
{

    public async Task Handle(DeleteUserReportMessageRequest request, CancellationToken cancellationToken)
    {
        var userReportData = await mediator.Send(new GetUserReportByChannelRequest(request.DiscordChannelId), cancellationToken);

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

        var userMessage = await mediator.Send(new GetUserReportMessageRequest(request.DiscordMessageId), cancellationToken);

        if (userMessage == null)
        {
            return;
        }

        db.Remove(userMessage);
        await db.SaveChangesAsync(cancellationToken);

        await mediator.Send(new InvalidateGetUserReportMessageRequest(userMessage.Id), cancellationToken);
        //todo log update on an audit log? maybe trigger?

        await mediator.Send(
            new DeleteUserReportDiscordMessageRequest(
                webhookId,
                webhookToken,
                userMessage.DiscordProxyMessageId),
            cancellationToken
        );
    }
}
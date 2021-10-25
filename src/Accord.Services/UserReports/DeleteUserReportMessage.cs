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
    : IRequest<ServiceResponse>;

public sealed record DeleteUserReportMessageRequest(
        ulong DiscordMessageId,
        ulong DiscordChannelId,
        UserReportChannelType DiscordChannelType)
    : IRequest<ServiceResponse>;

public class DeleteUserReportMessageHandler : IRequestHandler<DeleteUserReportMessageRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public DeleteUserReportMessageHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(DeleteUserReportMessageRequest request, CancellationToken cancellationToken)
    {
        var userReportData = await _mediator.Send(new GetUserReportByChannelRequest(request.DiscordChannelId), cancellationToken);

        if (userReportData == null)
        {
            return ServiceResponse.Fail<(UserReport, UserReportMessage)>("Couldn't retrieve report's information");
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
            return ServiceResponse.Fail<(UserReport, UserReportMessage)>("Couldn't retrieve message's information");
        }

        _db.Remove(userMessage);
        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateGetUserReportMessageRequest(userMessage.Id), cancellationToken);
        //todo log update on an audit log? maybe trigger?

        return await _mediator.Send(
            new DeleteUserReportDiscordMessageRequest(
                webhookId,
                webhookToken,
                userMessage.DiscordProxyMessageId),
            cancellationToken
        );
    }
}
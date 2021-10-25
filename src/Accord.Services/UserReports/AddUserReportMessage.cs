using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model.UserReports;
using MediatR;

namespace Accord.Services.UserReports;

public sealed record AddUserReportMessageRequest(ulong DiscordGuildId,
        ulong DiscordMessageId,
        ulong DiscordUserId,
        ulong DiscordChannelId,
        UserReportChannelType DiscordChannelType,
        string DiscordMessageContent,
        List<DiscordAttachmentDto> DiscordAttachments,
        ulong? DiscordMessageReferenceId,
        DateTimeOffset SentDateTime)
    : IRequest<ServiceResponse>;

public sealed record RelayUserReportMessageRequest(ulong DiscordGuildId,
        ulong ToDiscordChannelId,
        ulong OriginalDiscordMessageId,
        ulong DiscordProxyWebhookId,
        string DiscordProxyWebhookToken,
        string Content,
        List<DiscordAttachmentDto> DiscordAttachments,
        ulong AuthorDiscordUserId,
        ulong? DiscordMessageReferenceId,
        DateTimeOffset SentDateTime)
    : IRequest<ServiceResponse>;

public class AddUserReportMessageHandler : IRequestHandler<AddUserReportMessageRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public AddUserReportMessageHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(AddUserReportMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.DiscordChannelType == UserReportChannelType.None)
            return ServiceResponse.Fail("DiscordChannelType was none");

        var userReportData = await _mediator.Send(new GetUserReportByChannelRequest(request.DiscordChannelId), cancellationToken);

        if (userReportData == null)
        {
            return ServiceResponse.Fail("Couldn't retrieve report's information");
        }

        ulong webhookId;
        ulong channelId;
        string webhookToken;
        if (request.DiscordChannelType == UserReportChannelType.Inbox)
        {
            webhookId = userReportData.OutboxDiscordMessageProxyWebhookId;
            webhookToken = userReportData.OutboxDiscordMessageProxyWebhookToken;
            channelId = userReportData.OutboxDiscordChannelId;
        }
        else if (request.DiscordChannelType == UserReportChannelType.Outbox)
        {
            webhookId = userReportData.InboxDiscordMessageProxyWebhookId;
            webhookToken = userReportData.InboxDiscordMessageProxyWebhookToken;
            channelId = userReportData.InboxDiscordChannelId;
        }
        else
            throw new NotSupportedException($"Discord Channel Type {request.DiscordChannelType} is not supported");

        var userReport = new UserReportMessage
        {
            Id = request.DiscordMessageId,
            UserReportId = userReportData.Id,
            SentDateTime = request.SentDateTime,
            AuthorUserId = request.DiscordUserId,
            Content = request.DiscordMessageContent,
        };

        _db.Add(userReport);
        await _db.SaveChangesAsync(cancellationToken);

        //todo: add userReport messageId to the request
        await _mediator.Send(new RelayUserReportMessageRequest(
            request.DiscordGuildId,
            channelId,
            request.DiscordMessageId,
            webhookId,
            webhookToken,
            request.DiscordMessageContent,
            request.DiscordAttachments,
            request.DiscordUserId,
            request.DiscordMessageReferenceId,
            request.SentDateTime), cancellationToken);

        return ServiceResponse.Ok();
    }
}
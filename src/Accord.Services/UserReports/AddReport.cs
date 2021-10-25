using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model.UserReports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record AddReportRequest(ulong DiscordUserId,
    ulong OutboxDiscordChannelId,
    ulong OutboxDiscordMessageProxyWebhookId,
    string OutboxDiscordMessageProxyWebhookToken,
    ulong InboxDiscordChannelId,
    ulong InboxDiscordMessageProxyWebhookId,
    string InboxDiscordMessageProxyWebhookToken) : IRequest;

public class AddReportHandler : AsyncRequestHandler<AddReportRequest>
{
    private readonly AccordContext _db;

    public AddReportHandler(AccordContext db)
    {
        _db = db;
    }

    public async Task<ExistingOutboxReportForUserDto> Handle(GetExistingOutboxReportForUserRequest request, CancellationToken cancellationToken)
    {
        var existingOutboxChannelId = await _db.UserReports
            .Where(x => x.OpenedByUserId == request.DiscordUserId)
            .Where(x => x.ClosedDateTime == null)
            .Select(x => (ulong?)x.OutboxDiscordChannelId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (existingOutboxChannelId is null)
        {
            return new ExistingOutboxReportForUserDto(false, null);
        }

        return new ExistingOutboxReportForUserDto(true, existingOutboxChannelId);
    }

    protected override async Task Handle(AddReportRequest request, CancellationToken cancellationToken)
    {
        var report = new UserReport
        {
            OpenedByUserId = request.DiscordUserId,
            OutboxDiscordChannelId = request.OutboxDiscordChannelId,
            OutboxDiscordMessageProxyWebhookId = request.OutboxDiscordMessageProxyWebhookId,
            OutboxDiscordMessageProxyWebhookToken = request.OutboxDiscordMessageProxyWebhookToken,
            InboxDiscordChannelId = request.InboxDiscordChannelId,
            InboxDiscordMessageProxyWebhookId = request.InboxDiscordMessageProxyWebhookId,
            InboxDiscordMessageProxyWebhookToken = request.InboxDiscordMessageProxyWebhookToken,
            OpenedDateTime = DateTimeOffset.Now,
        };

        _db.Add(report);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
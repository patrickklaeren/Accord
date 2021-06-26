using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model.UserReports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports
{
    public sealed record AddUserReportOutboxMessageRequest(ulong DiscordGuildId, ulong DiscordMessageId, ulong DiscordUserId, ulong DiscordChannelId, string DiscordMessageContent, DateTimeOffset SentDateTime) 
        : IRequest<ServiceResponse>;

    public class AddUserReportOutboxMessageHandler : IRequestHandler<AddUserReportOutboxMessageRequest, ServiceResponse>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public AddUserReportOutboxMessageHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<ServiceResponse> Handle(AddUserReportOutboxMessageRequest request, 
            CancellationToken cancellationToken)
        {
            var userReportData = await _db.UserReports
                .Where(x => x.OutboxDiscordChannelId == request.DiscordChannelId)
                .Select(x => new
                {
                    x.Id,
                    x.InboxDiscordChannelId,
                })
                .SingleAsync(cancellationToken: cancellationToken);

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

            await _mediator.Send(new RelayUserReportMessageRequest(request.DiscordGuildId,
                userReportData.InboxDiscordChannelId, request.DiscordMessageContent,
                request.DiscordUserId, request.SentDateTime), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}

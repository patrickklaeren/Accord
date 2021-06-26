using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    public class UserReportsMessageResponder : IResponder<IMessageCreate>, 
        IResponder<IMessageDelete>, 
        IResponder<IMessageDeleteBulk>
    {
        private readonly IEventQueue _eventQueue;
        private readonly IMediator _mediator;

        public UserReportsMessageResponder(IEventQueue eventQueue, IMediator mediator)
        {
            _eventQueue = eventQueue;
            _mediator = mediator;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
                return Result.FromSuccess();

            var reportChannelType = await _mediator.Send(new GetUserReportChannelTypeRequest(gatewayEvent.ChannelID.Value), ct);

            if (reportChannelType == UserReportChannelType.None)
                return Result.FromSuccess();

            if (reportChannelType == UserReportChannelType.Outbox)
            {
                await _eventQueue.Queue(new AddUserReportOutboxMessageEvent(gatewayEvent.GuildID.Value.Value, gatewayEvent.ID.Value,
                    gatewayEvent.Author.ID.Value, gatewayEvent.ChannelID.Value, gatewayEvent.Content,
                    gatewayEvent.Timestamp));
            }
            else if (reportChannelType == UserReportChannelType.Inbox)
            {
                await _eventQueue.Queue(new AddUserReportInboxMessageEvent(gatewayEvent.GuildID.Value.Value, gatewayEvent.ID.Value,
                    gatewayEvent.Author.ID.Value, gatewayEvent.ChannelID.Value, gatewayEvent.Content,
                    gatewayEvent.Timestamp));
            }

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var reportChannelType = await _mediator.Send(new GetUserReportChannelTypeRequest(gatewayEvent.ChannelID.Value), ct);

            if (reportChannelType == UserReportChannelType.None)
                return Result.FromSuccess();

            if (reportChannelType == UserReportChannelType.Outbox)
            {
                await _eventQueue.Queue(new DeleteUserReportOutboxMessageEvent(gatewayEvent.ID.Value, DateTimeOffset.Now));
            }
            else if (reportChannelType == UserReportChannelType.Inbox)
            {
                await _eventQueue.Queue(new DeleteUserReportInboxMessageEvent(gatewayEvent.ID.Value, DateTimeOffset.Now));
            }

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDeleteBulk gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var reportChannelType = await _mediator.Send(new GetUserReportChannelTypeRequest(gatewayEvent.ChannelID.Value), ct);

            if (reportChannelType == UserReportChannelType.None)
                return Result.FromSuccess();

            foreach (var id in gatewayEvent.IDs)
            {
                if (reportChannelType == UserReportChannelType.Outbox)
                {
                    await _eventQueue.Queue(new DeleteUserReportOutboxMessageEvent(id.Value, DateTimeOffset.Now));
                }
                else if (reportChannelType == UserReportChannelType.Inbox)
                {
                    await _eventQueue.Queue(new DeleteUserReportInboxMessageEvent(id.Value, DateTimeOffset.Now));
                }
            }

            return Result.FromSuccess();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Services;
using Accord.Services.UserReports;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders
{
    // TODO Implement
    public class UserReportsMessageResponder
    //public class UserReportsMessageResponder : IResponder<IMessageCreate>,
    //    IResponder<IMessageDelete>,
    //    IResponder<IMessageDeleteBulk>,
    //    IResponder<IMessageUpdate>
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

            if (reportChannelType == UserReportChannelType.Inbox && !gatewayEvent.Content.StartsWith(">"))
                return Result.FromSuccess();

            string content = (reportChannelType == UserReportChannelType.Inbox
                ? gatewayEvent.Content.UnquoteAgentReportText()
                : gatewayEvent.Content).TrimStart();

            var attachments = gatewayEvent.Attachments
                .Select(x => new DiscordAttachmentDto(x.Url, x.Filename, x.ContentType.HasValue ? x.ContentType.Value : null))
                .ToList();

            ulong? messageReference = gatewayEvent.MessageReference.HasValue ? gatewayEvent.MessageReference.Value.MessageID.Value.Value : null;

            IUserEvent userEvent;
            if (reportChannelType == UserReportChannelType.Outbox)
            {
                userEvent = new AddUserReportOutboxMessageEvent(gatewayEvent.GuildID.Value.Value, gatewayEvent.ID.Value,
                    gatewayEvent.Author.ID.Value, gatewayEvent.ChannelID.Value, content, attachments, messageReference, gatewayEvent.Timestamp);
            }
            else
            {
                userEvent = new AddUserReportInboxMessageEvent(gatewayEvent.GuildID.Value.Value, gatewayEvent.ID.Value,
                    gatewayEvent.Author.ID.Value, gatewayEvent.ChannelID.Value, content, attachments, messageReference, gatewayEvent.Timestamp);
            }

            await _eventQueue.Queue(userEvent);

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var reportChannelType = await _mediator.Send(new GetUserReportChannelTypeRequest(gatewayEvent.ChannelID.Value), ct);

            if (reportChannelType == UserReportChannelType.None)
                return Result.FromSuccess();

            if (reportChannelType == UserReportChannelType.Outbox)
            {
                await _eventQueue.Queue(new DeleteUserReportOutboxMessageEvent(gatewayEvent.ID.Value, gatewayEvent.ChannelID.Value, DateTimeOffset.Now));
            }
            else if (reportChannelType == UserReportChannelType.Inbox)
            {
                await _eventQueue.Queue(new DeleteUserReportInboxMessageEvent(gatewayEvent.ID.Value, gatewayEvent.ChannelID.Value, DateTimeOffset.Now));
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
                    await _eventQueue.Queue(new DeleteUserReportOutboxMessageEvent(id.Value, gatewayEvent.ChannelID.Value, DateTimeOffset.Now));
                }
                else if (reportChannelType == UserReportChannelType.Inbox)
                {
                    await _eventQueue.Queue(new DeleteUserReportInboxMessageEvent(id.Value, gatewayEvent.ChannelID.Value, DateTimeOffset.Now));
                }
            }

            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (gatewayEvent.Author.Value.IsBot.HasValue || gatewayEvent.Author.Value.IsSystem.HasValue)
                return Result.FromSuccess();

            var reportChannelType = await _mediator.Send(new GetUserReportChannelTypeRequest(gatewayEvent.ChannelID.Value.Value), ct);

            if (reportChannelType == UserReportChannelType.None)
                return Result.FromSuccess();

            string content = (reportChannelType == UserReportChannelType.Inbox && gatewayEvent.Content.Value.StartsWith(">")
                ? gatewayEvent.Content.Value.UnquoteAgentReportText()
                : gatewayEvent.Content.Value).TrimStart();

            var attachments = (gatewayEvent.Attachments.HasValue ? gatewayEvent.Attachments.Value : new List<IAttachment>())
                .Select(x => new DiscordAttachmentDto(x.Url, x.Filename, x.ContentType.HasValue ? x.ContentType.Value : null))
                .ToList();

            if (reportChannelType == UserReportChannelType.Outbox)
            {
                await _eventQueue.Queue(new EditUserReportOutboxMessageEvent(gatewayEvent.ID.Value.Value, gatewayEvent.ChannelID.Value.Value, content,
                    attachments, DateTimeOffset.Now));
            }
            else if (reportChannelType == UserReportChannelType.Inbox)
            {
                await _eventQueue.Queue(new EditUserReportInboxMessageEvent(gatewayEvent.ID.Value.Value, gatewayEvent.ChannelID.Value.Value, content,
                    attachments, DateTimeOffset.Now));
            }

            return Result.FromSuccess();
        }
    }
}
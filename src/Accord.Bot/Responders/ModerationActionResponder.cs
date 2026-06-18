using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.UserHistories;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ModerationActionResponder(IMediator mediator) : IResponder<IGuildAuditLogEntryCreate>
{
    public async Task<Result> RespondAsync(IGuildAuditLogEntryCreate gatewayEvent, CancellationToken cancellationToken = new())
    {
        if (gatewayEvent.ActionType is not (AuditLogEvent.MemberKick or AuditLogEvent.MemberBanAdd or AuditLogEvent.MemberBanRemove)
            || gatewayEvent.TargetID is not { } targetIdString
            || !ulong.TryParse(targetIdString, out var targetUserId)
            || gatewayEvent.UserID is not { } actingUserSnowflake)
        {
            return Result.FromSuccess();
        }

        var actingUserId = actingUserSnowflake.Value;

        var type = gatewayEvent.ActionType switch
        {
            AuditLogEvent.MemberKick => UserHistoryType.Kick,
            AuditLogEvent.MemberBanAdd => UserHistoryType.Ban,
            AuditLogEvent.MemberBanRemove => UserHistoryType.Unban,
            _ => throw new InvalidOperationException("Unexpected audit log action type")
        };

        var reason = gatewayEvent.Reason.HasValue
            ? gatewayEvent.Reason.Value
            : "No reason provided";

        await mediator.Send(new AddUserHistoryRequest(
            targetUserId,
            actingUserId,
            reason,
            type
        ), cancellationToken);

        return Result.FromSuccess();
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.UserHistories;
using Accord.Services.Users;
using Humanizer;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Responders;

public class ModerationActionResponder(IMediator mediator,
    DiscordConfiguration discordConfiguration,
    PermissionUserFactory permissionUserFactory,
    IDiscordRestGuildAPI guildApi) 
    : IResponder<IGuildAuditLogEntryCreate>
{
    private readonly AuditLogEvent[] _typesToRespondTo =
    [
        AuditLogEvent.MemberKick,
        AuditLogEvent.MemberBanAdd,
        AuditLogEvent.MemberBanRemove,
        AuditLogEvent.MemberUpdate
    ];
    
    public async Task<Result> RespondAsync(IGuildAuditLogEntryCreate gatewayEvent, CancellationToken cancellationToken = new())
    {
        if (!_typesToRespondTo.Contains(gatewayEvent.ActionType)
            || gatewayEvent.TargetID is not { } targetIdString
            || !ulong.TryParse(targetIdString, out var targetUserId)
            || gatewayEvent.UserID is not { } actingUserSnowflake)
        {
            return Result.FromSuccess();
        }

        var actingUser = await permissionUserFactory.FromId(actingUserSnowflake.Value);

        var message = gatewayEvent.ActionType switch
        {
            AuditLogEvent.MemberKick => HandleSimpleAuditLog(UserHistoryType.Kick),
            AuditLogEvent.MemberBanAdd => HandleSimpleAuditLog(UserHistoryType.Ban),
            AuditLogEvent.MemberBanRemove => HandleSimpleAuditLog(UserHistoryType.Unban),
            AuditLogEvent.MemberUpdate => await HandleMemberUpdate(),
            _ => throw new InvalidOperationException("Unexpected audit log action type")
        };

        if (message is not null)
        {
            await mediator.Send(message, cancellationToken);            
        }

        return Result.FromSuccess();

        async Task<AddUserHistoryRequest?> HandleMemberUpdate()
        {
            if (!gatewayEvent.Changes.HasValue)
                return null;

            if (gatewayEvent
                .Changes
                .Value
                .Any(a => a.Key == "communication_disabled_until"))
            {
                var guildSnowflake = new Snowflake(discordConfiguration.GuildId);
                var targetUserSnowflake = new Snowflake(targetUserId);

                var guildMember = await guildApi.GetGuildMemberAsync(guildSnowflake,
                    targetUserSnowflake,
                    cancellationToken);

                if (!guildMember.IsSuccess 
                    || !guildMember.Entity.CommunicationDisabledUntil.HasValue)
                    return null;
                
                var timedOutUntil = guildMember.Entity.CommunicationDisabledUntil.Value;

                var reasonForTimeout = gatewayEvent.Reason.HasValue
                    ? gatewayEvent.Reason.Value
                    : "No reason provided";
                
                if (reasonForTimeout.StartsWith("On behalf of "))
                {
                    var split = reasonForTimeout
                        .Replace("On behalf of ", string.Empty)
                        .Split('-', StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries);

                    if (ulong.TryParse(split[0], out var behalfOfUserId))
                    {
                        actingUser = await permissionUserFactory.FromId(behalfOfUserId);
                    }

                    if (split.Length > 1)
                    {
                        reasonForTimeout = split[1];
                    }
                }
                
                if (timedOutUntil is not null)
                {
                    var timedOutFrom = gatewayEvent.ID.Timestamp;
                    var durationOfTimeout = timedOutUntil - timedOutFrom;
                    var content = $"Timed out until {timedOutUntil.Value:dd/MM/yyyy HH:mm} ({durationOfTimeout.Value.Humanize()}) - {reasonForTimeout}";
                
                    return new AddUserHistoryRequest(
                        targetUserId,
                        actingUser,
                        content,
                        UserHistoryType.Mute);
                }

                return new AddUserHistoryRequest(
                    targetUserId,
                    actingUser,
                    "Removed timeout",
                    UserHistoryType.Unmute);
            }

            if (gatewayEvent
                .Changes
                .Value
                .Any(a => a.Key == "mute"))
            {
                var change = gatewayEvent
                    .Changes
                    .Value
                    .First(a => a.Key == "mute");

                var isMuted = change.NewValue is { HasValue: true, Value: "true" };

                if (isMuted)
                {
                    var autoUnmuteAtDateTime = await mediator.Send(new ScheduleVoiceAutoUnmuteForUserRequest(targetUserId), cancellationToken);
                    var content = "Permanently muted in voice";

                    if (autoUnmuteAtDateTime is not null)
                    {
                        content = $"Muted in voice until {autoUnmuteAtDateTime.Value:dd/MM/yyyy HH:mm} ({autoUnmuteAtDateTime.Value.Humanize()})";
                    }
                    
                    return new AddUserHistoryRequest(
                        targetUserId,
                        actingUser,
                        content,
                        UserHistoryType.Mute);   
                }

                await mediator.Publish(new UnscheduleVoiceAutoUnmuteForUserRequest(targetUserId), cancellationToken);
                
                return new AddUserHistoryRequest(
                    targetUserId,
                    actingUser,
                    "Unmuted in voice",
                    UserHistoryType.Unmute);
            }

            return null;
        }

        AddUserHistoryRequest HandleSimpleAuditLog(UserHistoryType type)
        {
            var content = gatewayEvent.Reason.HasValue
                ? gatewayEvent.Reason.Value
                : "No reason provided";

            return new AddUserHistoryRequest(
                targetUserId,
                actingUser,
                content,
                type);
        }
    }
}

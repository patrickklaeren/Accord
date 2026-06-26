using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.VoiceSessions;

public sealed record FinishVoiceSessionRequest(ulong DiscordGuildId, ulong DiscordUserId, string DiscordSessionId,
    DateTimeOffset DisconnectedDateTime) : IRequest, IEnsureUserExistsRequest;

public sealed record LeftVoiceRequest(ulong DiscordGuildId, ulong DiscordUserId,
    ulong DiscordChannelId, DateTimeOffset ConnectedDateTime, DateTimeOffset DisconnectedDateTime,
    string DiscordSessionId) : INotification;

// Discord Session ID is a session ID for the voice state itself
// it does not necessarily change upon every new voice session, i.e.
// a user connects and disconnects from a VC does not constitute a
// unique session ID, the same session ID could then be used for
// subsequent voice connections. This sucks.
// https://discord.com/developers/docs/resources/voice#voice-state-object
internal class FinishVoiceSessionHandler(AccordContext db, IMediator mediator) : IRequestHandler<FinishVoiceSessionRequest>
{
    public async Task Handle(FinishVoiceSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await db.VoiceConnections
            .Where(x => x.DiscordSessionId == request.DiscordSessionId)
            .Where(x => x.EndDateTime == null)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (session is null)
        {
            return;
        }

        session.EndDateTime = request.DisconnectedDateTime;
        session.MinutesInVoiceChannel = Math.Round((request.DisconnectedDateTime - session.StartDateTime).TotalMinutes, 2);

        await db.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new LeftVoiceRequest(request.DiscordGuildId, session.UserId, session.DiscordChannelId, session.StartDateTime,
            session.EndDateTime.Value, request.DiscordSessionId), cancellationToken);
    }
}
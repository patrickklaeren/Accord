using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.VoiceSessions;

public sealed record StartVoiceSessionRequest(ulong DiscordGuildId, ulong DiscordUserId, 
    ulong DiscordChannelId, string DiscordSessionId, DateTimeOffset ConnectedDateTime) : IRequest, IEnsureUserExistsRequest;
public sealed record FinishVoiceSessionRequest(ulong DiscordGuildId, ulong DiscordUserId, string DiscordSessionId, 
    DateTimeOffset DisconnectedDateTime) : IRequest, IEnsureUserExistsRequest;

public sealed record JoinedVoiceRequest(ulong DiscordGuildId, ulong DiscordUserId, 
    ulong DiscordChannelId, DateTimeOffset ConnectedDateTime, string DiscordSessionId) : IRequest, IEnsureUserExistsRequest;
public sealed record LeftVoiceRequest(ulong DiscordGuildId, ulong DiscordUserId, 
    ulong DiscordChannelId, DateTimeOffset ConnectedDateTime, DateTimeOffset DisconnectedDateTime, 
    string DiscordSessionId) : IRequest, IEnsureUserExistsRequest;

// Discord Session ID is a session ID for the voice state itself
// it does not necessarily changed upon every new voice session, i.e.
// a user connects and disconnects from a VC does not constitute a
// unique session ID, the same session ID could then be used for
// subsequent voice connections. This sucks.
// https://discord.com/developers/docs/resources/voice#voice-state-object

public class StartVoiceSessionHandler : AsyncRequestHandler<StartVoiceSessionRequest>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public StartVoiceSessionHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    protected override async Task Handle(StartVoiceSessionRequest request, CancellationToken cancellationToken)
    {
        if (await _db.VoiceConnections.AnyAsync(a => a.DiscordSessionId == request.DiscordSessionId && a.EndDateTime == null, cancellationToken))
        {
            return;
        }

        var session = new VoiceSession
        {
            UserId = request.DiscordUserId,
            DiscordChannelId = request.DiscordChannelId,
            StartDateTime = request.ConnectedDateTime,
            DiscordSessionId = request.DiscordSessionId,
        };

        _db.Add(session);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new JoinedVoiceRequest(request.DiscordGuildId, request.DiscordUserId, request.DiscordChannelId, request.ConnectedDateTime, request.DiscordSessionId), cancellationToken);
    }
}

public class FinishVoiceSessionHandler : AsyncRequestHandler<FinishVoiceSessionRequest>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public FinishVoiceSessionHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    protected override async Task Handle(FinishVoiceSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.VoiceConnections
            .Where(x => x.DiscordSessionId == request.DiscordSessionId)
            .Where(x => x.EndDateTime == null)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (session is null)
        {
            return;
        }

        session.EndDateTime = request.DisconnectedDateTime;
        session.MinutesInVoiceChannel = Math.Round((request.DisconnectedDateTime - session.StartDateTime).TotalMinutes, 2);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new LeftVoiceRequest(request.DiscordGuildId, session.UserId, session.DiscordChannelId, session.StartDateTime, 
            session.EndDateTime.Value, request.DiscordSessionId), cancellationToken);
    }
}
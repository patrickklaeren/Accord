using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.VoiceSessions;

public sealed record StartVoiceSessionRequest(ulong DiscordGuildId, ulong DiscordUserId, ulong DiscordChannelId, string DiscordSessionId, DateTimeOffset ConnectedDateTime) : IRequest<ServiceResponse>;
public sealed record FinishVoiceSessionRequest(ulong DiscordGuildId, string DiscordSessionId, DateTimeOffset DisconnectedDateTime) : IRequest<ServiceResponse>;

public sealed record JoinedVoiceRequest(ulong DiscordGuildId, ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset ConnectedDateTime, string DiscordSessionId) : IRequest;
public sealed record LeftVoiceRequest(ulong DiscordGuildId, ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset ConnectedDateTime, DateTimeOffset DisconnectedDateTime, string DiscordSessionId) : IRequest;

public class VoiceSessionHandler : IRequestHandler<StartVoiceSessionRequest, ServiceResponse>, IRequestHandler<FinishVoiceSessionRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public VoiceSessionHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    // Discord Session ID is a session ID for the voice state itself
    // it does not necessarily changed upon every new voice session, i.e.
    // a user connects and disconnects from a VC does not constitute a
    // unique session ID, the same session ID could then be used for
    // subsequent voice connections. This sucks.
    // https://discord.com/developers/docs/resources/voice#voice-state-object

    public async Task<ServiceResponse> Handle(StartVoiceSessionRequest request, CancellationToken cancellationToken)
    {
        if (await _db.VoiceConnections.AnyAsync(a => a.DiscordSessionId == request.DiscordSessionId && a.EndDateTime == null, cancellationToken))
        {
            return ServiceResponse.Fail("Session already exists");
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

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> Handle(FinishVoiceSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _db.VoiceConnections
            .Where(x => x.DiscordSessionId == request.DiscordSessionId)
            .Where(x => x.EndDateTime == null)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (session is null)
        {
            return ServiceResponse.Fail("Session does not exist");
        }

        session.EndDateTime = request.DisconnectedDateTime;
        session.MinutesInVoiceChannel = Math.Round((request.DisconnectedDateTime - session.StartDateTime).TotalMinutes, 2);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new LeftVoiceRequest(request.DiscordGuildId, session.UserId, session.DiscordChannelId, session.StartDateTime, 
            session.EndDateTime.Value, request.DiscordSessionId), cancellationToken);

        return ServiceResponse.Ok();
    }
}
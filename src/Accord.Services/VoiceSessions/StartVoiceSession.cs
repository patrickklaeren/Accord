using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.VoiceSessions;

public sealed record StartVoiceSessionRequest(ulong DiscordGuildId, ulong DiscordUserId,
    ulong DiscordChannelId, string DiscordSessionId, DateTimeOffset ConnectedDateTime) : IRequest, IEnsureUserExistsRequest;

public sealed record JoinedVoiceRequest(ulong DiscordGuildId, ulong DiscordUserId,
    ulong DiscordChannelId, DateTimeOffset ConnectedDateTime, string DiscordSessionId) : INotification;

// Discord Session ID is a session ID for the voice state itself
// it does not necessarily change upon every new voice session, i.e.
// a user connects and disconnects from a VC does not constitute a
// unique session ID, the same session ID could then be used for
// subsequent voice connections. This sucks.
// https://discord.com/developers/docs/resources/voice#voice-state-object
internal class StartVoiceSessionHandler(AccordContext db,
    UserService userService,
    IMediator mediator) : IRequestHandler<StartVoiceSessionRequest>
{
    public async Task Handle(StartVoiceSessionRequest request, CancellationToken cancellationToken)
    {
        if (await db.VoiceConnections.AnyAsync(a => a.DiscordSessionId == request.DiscordSessionId && a.EndDateTime == null, cancellationToken))
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

        db.Add(session);

        await db.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new JoinedVoiceRequest(request.DiscordGuildId, request.DiscordUserId, request.DiscordChannelId, request.ConnectedDateTime, request.DiscordSessionId), cancellationToken);
        await userService.TryAutoVoiceUnmuteUser(request.DiscordUserId, cancellationToken);
    }
}
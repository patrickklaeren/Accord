using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.VoiceSessions
{
    public sealed record StartVoiceSessionRequest(ulong DiscordUserId, ulong DiscordChannelId, string DiscordSessionId, DateTimeOffset ConnectedDateTime) : IRequest<ServiceResponse>;
    public sealed record FinishVoiceSessionRequest(string DiscordSessionId, DateTimeOffset DisconnectedDateTime) : IRequest<ServiceResponse>;

    public class VoiceSessionHandler : IRequestHandler<StartVoiceSessionRequest, ServiceResponse>, IRequestHandler<FinishVoiceSessionRequest, ServiceResponse>
    {
        private readonly AccordContext _db;

        public VoiceSessionHandler(AccordContext db)
        {
            _db = db;
        }

        public async Task<ServiceResponse> Handle(StartVoiceSessionRequest request, CancellationToken cancellationToken)
        {
            if (await _db.VoiceConnections.AnyAsync(a => a.DiscordSessionId == request.DiscordSessionId, cancellationToken: cancellationToken))
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

            return ServiceResponse.Ok();
        }

        public async Task<ServiceResponse> Handle(FinishVoiceSessionRequest request, CancellationToken cancellationToken)
        {
            var session = await _db.VoiceConnections
                .Where(x => x.DiscordSessionId == request.DiscordSessionId)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (session is null)
            {
                return ServiceResponse.Fail("Session does not exist");
            }

            session.EndDateTime = request.DisconnectedDateTime;
            session.MinutesInVoiceChannel = Math.Round((request.DisconnectedDateTime - session.StartDateTime).TotalMinutes, 2);

            await _db.SaveChangesAsync(cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}

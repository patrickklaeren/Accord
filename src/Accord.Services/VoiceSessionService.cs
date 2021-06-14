using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services
{
    public class VoiceSessionService
    {
        private readonly AccordContext _db;

        public VoiceSessionService(AccordContext db)
        {
            _db = db;
        }

        public async Task<ServiceResponse> Start(ulong discordUserId, ulong discordChannelId, string discordSessionId, DateTimeOffset connectedDateTime)
        {
            if (await _db.VoiceConnections.AnyAsync(a => a.DiscordSessionId == discordSessionId))
            {
                return ServiceResponse.Fail("Session already exists");
            }

            var session = new VoiceSession
            {
                UserId = discordUserId,
                DiscordChannelId = discordChannelId,
                StartDateTime = connectedDateTime,
                DiscordSessionId = discordSessionId,
            };

            _db.Add(session);

            await _db.SaveChangesAsync();

            return ServiceResponse.Ok();
        }

        public async Task<ServiceResponse> Finish(string discordSessionId, DateTimeOffset disconnectedDateTime)
        {
            var session = await _db.VoiceConnections
                .Where(x => x.DiscordSessionId == discordSessionId)
                .SingleOrDefaultAsync();

            if (session is null)
            {
                return ServiceResponse.Fail("Session does not exist");
            }

            session.EndDateTime = disconnectedDateTime;
            session.MinutesInVoiceChannel = Math.Round((disconnectedDateTime - session.StartDateTime).TotalMinutes, 2);

            await _db.SaveChangesAsync();

            return ServiceResponse.Ok();
        }
    }
}

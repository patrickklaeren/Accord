using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users
{
    public sealed record UpdateUserRequest(ulong DiscordUserId, string DiscordUsername, string DiscordDiscriminator, string? DiscordNickname, DateTimeOffset? JoinedDateTime) : IRequest { }

    public class UpdateUserHandler : AsyncRequestHandler<UpdateUserRequest>
    {
        private readonly AccordContext _db;

        public UpdateUserHandler(AccordContext db)
        {
            _db = db;
        }

        protected override async Task Handle(UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .SingleOrDefaultAsync(x => x.Id == request.DiscordUserId, cancellationToken: cancellationToken);

            if (user is null)
                return;

            user.JoinedGuildDateTime = request.JoinedDateTime;
            user.UsernameWithDiscriminator = $"{request.DiscordUsername}#{request.DiscordDiscriminator}";
            user.Nickname = request.DiscordNickname;

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

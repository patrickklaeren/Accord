using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.NamePatterns;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users
{
    public sealed record UpdateUserRequest(ulong DiscordGuildId, ulong DiscordUserId, string DiscordUsername, string DiscordDiscriminator, string? DiscordNickname, DateTimeOffset? JoinedDateTime) : IRequest { }

    public class UpdateUserHandler : AsyncRequestHandler<UpdateUserRequest>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public UpdateUserHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
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

            await _mediator.Send(new ScanNameForPatternsRequest(request.DiscordGuildId, user.Id, user.UsernameWithDiscriminator, user.Nickname), cancellationToken);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Helpers;
using Accord.Services.Moderation;
using Accord.Services.NamePatterns;
using Accord.Services.Raid;
using MediatR;

namespace Accord.Services.Users
{
    public sealed record AddUserRequest(ulong DiscordGuildId,
        ulong DiscordUserId,
        string DiscordUsername,
        string DiscordDiscriminator,
        string? DiscordAvatarUrl,
        string? DiscordNickname,
        DateTimeOffset JoinedDateTime) : IRequest;

    public class AddUserHandler : AsyncRequestHandler<AddUserRequest>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public AddUserHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        protected override async Task Handle(AddUserRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

            if (userExists)
                return;

            var user = new User
            {
                Id = request.DiscordUserId,
                FirstSeenDateTime = request.JoinedDateTime,
                JoinedGuildDateTime = request.JoinedDateTime,
                UsernameWithDiscriminator = $"{request.DiscordUsername}#{request.DiscordDiscriminator}",
                Nickname = request.DiscordNickname,
            };

            _db.Add(user);

            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateUserExistsRequest(request.DiscordUserId), cancellationToken);

            await _mediator.Send(new ScanNameForPatternsRequest(request.DiscordGuildId, 
                new GuildUserDto(user.Id, request.DiscordUsername, request.DiscordDiscriminator, user.Nickname, request.DiscordAvatarUrl, user.JoinedGuildDateTime.Value)), cancellationToken);
        }
    }
}

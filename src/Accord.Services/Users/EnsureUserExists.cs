using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.Users;

public sealed record EnsureUserExistsRequest(ulong DiscordUserId) : IRequest;

[AutoConstructor]
public partial class EnsureUserExistsHandler : AsyncRequestHandler<EnsureUserExistsRequest>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    protected override async Task Handle(EnsureUserExistsRequest request, CancellationToken cancellationToken)
    {
        var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

        if (userExists)
            return;

        var user = new User
        {
            Id = request.DiscordUserId,
            FirstSeenDateTime = DateTimeOffset.Now,
        };

        _db.Add(user);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateUserExistsRequest(request.DiscordUserId), cancellationToken);
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Xp;

public sealed record AddXpForMessageRequest(ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset MessageSentDateTime) : IRequest<ServiceResponse>;

public class AddXp : IRequestHandler<AddXpForMessageRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public AddXp(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(AddXpForMessageRequest request, CancellationToken cancellationToken)
    {
        var channels = await _mediator.Send(new GetChannelsWithFlagRequest(ChannelFlagType.IgnoredFromXp));

        if (channels.Any(id => id == request.DiscordChannelId))
            return ServiceResponse.Fail("Channel is ignored from XP");

        var user = await _db.Users.SingleAsync(x => x.Id == request.DiscordUserId, cancellationToken: cancellationToken);

        if (user.LastSeenDateTime.AddSeconds(10) > request.MessageSentDateTime)
        {
            user.Xp += 5;
        }

        user.LastSeenDateTime = request.MessageSentDateTime;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserHistories;

public sealed record AddUserHistoryRequest(ulong DiscordUserId, ulong AddedByUserId, string Content) 
    : IRequest<ServiceResponse>;

public class AddUserHistoryHandler(AccordContext db, IMediator mediator) 
    : IRequestHandler<AddUserHistoryRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(AddUserHistoryRequest request, CancellationToken cancellationToken)
    {
        var userExists = await mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail("User does not exist");

        var history = new UserHistory
        {
            Type = UserHistoryType.Generic,
            Content = request.Content,
            UserId = request.DiscordUserId,
            AddedByUserId = request.AddedByUserId,
            AddedDateTime = DateTimeOffset.UtcNow,
        };

        db.Add(history);

        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}

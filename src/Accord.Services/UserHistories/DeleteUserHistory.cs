using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHistories;

public sealed record DeleteUserHistoryRequest(int UserHistoryId, PermissionUser DeletedByUser) 
    : IRequest<ServiceResponse>;

public class DeleteUserHistoryHandler(AccordContext db, IMediator mediator) 
    : IRequestHandler<DeleteUserHistoryRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(DeleteUserHistoryRequest request, CancellationToken cancellationToken)
    {
        var history = await db.UserHistories
            .Where(x => x.Id == request.UserHistoryId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (history is null)
        {
            return ServiceResponse.Fail("No history found");
        }

        if (history.Type is not (UserHistoryType.Note or UserHistoryType.Warning))
        {
            return ServiceResponse.Fail("Only warnings and generic histories can be removed");
        }
        
        if (history.AddedByUserId != request.DeletedByUser.DiscordUserId 
            && !request.DeletedByUser.IsAdministrator)
        {
            var hasPermission = await mediator.Send(new UserHasPermissionRequest(request.DeletedByUser, PermissionType.ManageNotes),
                cancellationToken);

            if (!hasPermission)
            {
                return ServiceResponse.Fail("Missing permission");
            }
        }

        db.Remove(history);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResponse.Ok();
    }
}

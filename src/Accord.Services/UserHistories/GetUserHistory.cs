using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHistories;

public sealed record GetUserHistoryRequest(int NoteId) 
    : IRequest<UserHistoryDto?>;

public class GetUserHistoryHandler(AccordContext db) 
    : IRequestHandler<GetUserHistoryRequest, UserHistoryDto?>
{
    public async Task<UserHistoryDto?> Handle(GetUserHistoryRequest request, CancellationToken cancellationToken)
    {
        var history = await db.UserHistories
            .Where(x => x.Id == request.NoteId)
            .Select(x => new UserHistoryDto(x.Id,
                x.Type,
                x.Content,
                x.AddedDateTime,
                x.UserId,
                x.AddedByUserId))
            .SingleOrDefaultAsync(cancellationToken);

        return history;
    }
}

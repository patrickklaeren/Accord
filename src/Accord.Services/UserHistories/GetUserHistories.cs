using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHistories;

public sealed record GetUserHistoriesRequest(ulong DiscordUserId) 
    : IRequest<IReadOnlyCollection<UserHistoryDto>>;

public sealed record UserHistoryDto(int Id, 
    UserHistoryType Type, 
    string Content,
    DateTimeOffset AddedDateTime,
    ulong AddedByUserId);

public class GetUserHistoriesHandler(AccordContext db) 
    : IRequestHandler<GetUserHistoriesRequest, IReadOnlyCollection<UserHistoryDto>>
{
    public async Task<IReadOnlyCollection<UserHistoryDto>> Handle(GetUserHistoriesRequest request, CancellationToken cancellationToken)
    {
        var histories = await db.UserHistories
            .Where(x => x.UserId == request.DiscordUserId)
            .OrderByDescending(x => x.AddedDateTime)
            .Select(x => new UserHistoryDto(x.Id,
                x.Type,
                x.Content,
                x.AddedDateTime,
                x.AddedByUserId))
            .ToListAsync(cancellationToken);

        return histories;
    }
}

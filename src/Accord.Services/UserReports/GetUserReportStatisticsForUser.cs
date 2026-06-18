using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportsStatisticsForUserRequest(ulong DiscordUserId) : IRequest<int>;

public class GetUserReportStatisticsForUserHandler(AccordContext db) : IRequestHandler<GetUserReportsStatisticsForUserRequest, int>
{

    public async Task<int> Handle(GetUserReportsStatisticsForUserRequest request, CancellationToken cancellationToken)
    {
        return await db.UserReports
            .Where(x => x.OpenedByUserId == request.DiscordUserId)
            .Where(x => x.ClosedDateTime != null)
            .CountAsync(cancellationToken: cancellationToken);
    }
}
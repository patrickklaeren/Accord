using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportsStatisticsForUserRequest(ulong DiscordUserId) : IRequest<int>;

[AutoConstructor]
public partial class GetUserReportStatisticsForUserHandler : IRequestHandler<GetUserReportsStatisticsForUserRequest, int>
{
    private readonly AccordContext _db;

    public async Task<int> Handle(GetUserReportsStatisticsForUserRequest request, CancellationToken cancellationToken)
    {
        return await _db.UserReports
            .Where(x => x.OpenedByUserId == request.DiscordUserId)
            .Where(x => x.ClosedDateTime != null)
            .CountAsync(cancellationToken: cancellationToken);
    }
}
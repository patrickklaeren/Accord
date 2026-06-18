using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetIsUserReportsEnabledRequest : IRequest<bool>;

public class GetIsUserReportsEnabled(AccordContext db) : IRequestHandler<GetIsUserReportsEnabledRequest, bool>
{

    public async Task<bool> Handle(GetIsUserReportsEnabledRequest request, CancellationToken cancellationToken)
    {
        var runOption = await db.RunOptions
            .Where(x => x.Type == RunOptionType.UserReportsEnabled)
            .Select(x => x.Value)
            .SingleAsync(cancellationToken: cancellationToken);

        return bool.Parse(runOption);
    }
}
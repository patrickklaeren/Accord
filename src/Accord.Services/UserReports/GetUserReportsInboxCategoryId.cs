using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportsInboxCategoryIdRequest : IRequest<ulong?>;

public class GetUserReportsInboxCategoryIdHandler(AccordContext db) : IRequestHandler<GetUserReportsInboxCategoryIdRequest, ulong?>
{

    public async Task<ulong?> Handle(GetUserReportsInboxCategoryIdRequest request, CancellationToken cancellationToken)
    {
        var runOption = await db.RunOptions
            .Where(x => x.Type == RunOptionType.UserReportsInboxCategoryId)
            .Select(x => x.Value)
            .SingleAsync(cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(runOption))
            return null;

        return ulong.Parse(runOption);
    }
}
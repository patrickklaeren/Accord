using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportsAgentRoleIdRequest : IRequest<ulong?>;

public class GetUserReportsAgentRoleIdHandler(AccordContext db) : IRequestHandler<GetUserReportsAgentRoleIdRequest, ulong?>
{

    public async Task<ulong?> Handle(GetUserReportsAgentRoleIdRequest request, CancellationToken cancellationToken)
    {
        var runOption = await db.RunOptions
            .Where(x => x.Type == RunOptionType.UserReportsAgentRoleId)
            .Select(x => x.Value)
            .SingleAsync(cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(runOption))
            return null;

        return ulong.Parse(runOption);
    }
}
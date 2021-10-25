using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportsAgentRoleIdRequest : IRequest<ulong?>;

public class GetUserReportsAgentRoleIdHandler : IRequestHandler<GetUserReportsAgentRoleIdRequest, ulong?>
{
    private readonly AccordContext _db;

    public GetUserReportsAgentRoleIdHandler(AccordContext db)
    {
        _db = db;
    }

    public async Task<ulong?> Handle(GetUserReportsAgentRoleIdRequest request, CancellationToken cancellationToken)
    {
        var runOption = await _db.RunOptions
            .Where(x => x.Type == RunOptionType.UserReportsAgentRoleId)
            .Select(x => x.Value)
            .SingleAsync(cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(runOption))
            return null;

        return ulong.Parse(runOption);
    }
}
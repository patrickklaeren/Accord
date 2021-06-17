using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports
{
    public sealed record GetUserReportsReporterCategoryIdRequest : IRequest<ulong?>;

    public class GetUserReportsReporterCategoryIdHandler : IRequestHandler<GetUserReportsReporterCategoryIdRequest, ulong?>
    {
        private readonly AccordContext _db;

        public GetUserReportsReporterCategoryIdHandler(AccordContext db)
        {
            _db = db;
        }

        public async Task<ulong?> Handle(GetUserReportsReporterCategoryIdRequest request, CancellationToken cancellationToken)
        {
            var runOption = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.UserReportsReporterCategoryId)
                .Select(x => x.Value)
                .SingleAsync(cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(runOption))
                return null;

            return ulong.Parse(runOption);
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports
{
    public sealed record GetUserReportsModeratorCategoryIdRequest : IRequest<ulong?>;

    public class GetUserReportsModeratorCategoryIdHandler : IRequestHandler<GetUserReportsModeratorCategoryIdRequest, ulong?>
    {
        private readonly AccordContext _db;

        public GetUserReportsModeratorCategoryIdHandler(AccordContext db)
        {
            _db = db;
        }

        public async Task<ulong?> Handle(GetUserReportsModeratorCategoryIdRequest request, CancellationToken cancellationToken)
        {
            var runOption = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.UserReportsModeratorCategoryId)
                .Select(x => x.Value)
                .SingleAsync(cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(runOption))
                return null;

            return ulong.Parse(runOption);
        }
    }
}

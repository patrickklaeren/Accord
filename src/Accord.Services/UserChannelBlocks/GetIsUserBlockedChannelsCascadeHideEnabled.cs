using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.UserReports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserChannelBlocks
{
    public sealed record GetIsUserBlockedChannelsCascadeHideEnabledRequest : IRequest<bool>;

    public class GetIsUserBlockedChannelsCascadeHideEnabled : IRequestHandler<GetIsUserBlockedChannelsCascadeHideEnabledRequest, bool>
    {
        private readonly AccordContext _db;

        public GetIsUserBlockedChannelsCascadeHideEnabled(AccordContext db)
        {
            _db = db;
        }

        public async Task<bool> Handle(GetIsUserBlockedChannelsCascadeHideEnabledRequest request, CancellationToken cancellationToken)
        {
            var runOption = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.UserBlockedChannelsCascadeBlockEnabled)
                .Select(x => x.Value)
                .SingleAsync(cancellationToken: cancellationToken);

            return bool.Parse(runOption);
        }
    }
}
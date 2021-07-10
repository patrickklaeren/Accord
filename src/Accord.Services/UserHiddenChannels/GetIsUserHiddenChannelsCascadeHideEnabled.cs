using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHiddenChannels
{
    public sealed record GetIsUserHiddenChannelsCascadeHideEnabledRequest : IRequest<bool>;

    public class GetIsUserHiddenChannelsCascadeHideEnabled : IRequestHandler<GetIsUserHiddenChannelsCascadeHideEnabledRequest, bool>
    {
        private readonly AccordContext _db;

        public GetIsUserHiddenChannelsCascadeHideEnabled(AccordContext db)
        {
            _db = db;
        }

        public async Task<bool> Handle(GetIsUserHiddenChannelsCascadeHideEnabledRequest request, CancellationToken cancellationToken)
        {
            var runOption = await _db.RunOptions
                .Where(x => x.Type == RunOptionType.UserHiddenChannelsCascadeHideEnabled)
                .Select(x => x.Value)
                .SingleAsync(cancellationToken: cancellationToken);

            return bool.Parse(runOption);
        }
    }
}
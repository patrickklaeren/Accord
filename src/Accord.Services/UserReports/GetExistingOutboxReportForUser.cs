using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports
{
    public sealed record GetExistingOutboxReportForUserRequest(ulong DiscordUserId) : IRequest<ExistingOutboxReportForUserDto>;
    public sealed record ExistingOutboxReportForUserDto(bool HasExistingReport, ulong? OutboxDiscordChannelId);

    public class GetExistingOutboxReportForUserHandler : IRequestHandler<GetExistingOutboxReportForUserRequest, ExistingOutboxReportForUserDto>
    {
        private readonly AccordContext _db;

        public GetExistingOutboxReportForUserHandler(AccordContext db)
        {
            _db = db;
        }

        public async Task<ExistingOutboxReportForUserDto> Handle(GetExistingOutboxReportForUserRequest request, CancellationToken cancellationToken)
        {
            var existingOutboxChannelId = await _db.UserReports
                .Where(x => x.OpenedByUserId == request.DiscordUserId)
                .Where(x => x.ClosedDateTime == null)
                .Select(x => (ulong?)x.OutboxDiscordChannelId)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (existingOutboxChannelId is null)
            {
                return new ExistingOutboxReportForUserDto(false, null);
            }

            return new ExistingOutboxReportForUserDto(true, existingOutboxChannelId);
        }
    }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.NamePatterns
{
    public sealed record DeleteNamePatternRequest(PermissionUser User, string Pattern) : IRequest<ServiceResponse>;

    public class DeleteNamePatternHandler : IRequestHandler<AddNamePatternRequest, ServiceResponse>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public DeleteNamePatternHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<ServiceResponse> Handle(AddNamePatternRequest request, 
            CancellationToken cancellationToken)
        {
            var hasPermission = await _mediator.Send(new UserHasPermissionRequest(request.User, PermissionType.ManagePatterns), cancellationToken);

            if (!hasPermission)
            {
                return ServiceResponse.Fail("Missing permission");
            }

            var pattern = await _db.NamePatterns
                .Where(x => x.Pattern == request.Pattern)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (pattern is null)
            {
                return ServiceResponse.Fail("Pattern not found");
            }

            _db.Remove(pattern);

            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetNamePatternsRequest(), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}

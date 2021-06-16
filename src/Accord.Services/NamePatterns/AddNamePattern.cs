using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.NamePatterns
{
    public sealed record AddNamePatternRequest(PermissionUser User, string Pattern, PatternType Type, 
        OnNamePatternDiscovery OnDiscovery) : IRequest<ServiceResponse>;

    public class AddNamePatternHandler : IRequestHandler<AddNamePatternRequest, ServiceResponse>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public AddNamePatternHandler(AccordContext db, IMediator mediator)
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

            if (!IsValidRegex(request.Pattern))
            {
                return ServiceResponse.Fail("Pattern is not valid Regex");
            }

            var pattern = new NamePattern
            {
                Pattern = request.Pattern,
                Type = request.Type,
                OnDiscovery = request.OnDiscovery,
                AddedByUserId = request.User.DiscordUserId,
            };

            _db.Add(pattern);

            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetNamePatternsRequest(), cancellationToken);

            return ServiceResponse.Ok();
        }

        private bool IsValidRegex(string rawPattern)
        {
            try
            {
                _ = new Regex(rawPattern);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}

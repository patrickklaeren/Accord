using System.Linq;
using Accord.Domain.Model;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags
{
    public sealed record AddChannelFlagRequest(PermissionUser User, ChannelFlagType Flag, ulong DiscordChannelId) 
        : IRequest<ServiceResponse>;

    public class AddChannelFlagHandler : IRequestHandler<AddChannelFlagRequest, ServiceResponse>
    {
        private readonly AccordContext _db;
        private readonly IMediator _mediator;

        public AddChannelFlagHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<ServiceResponse> Handle(AddChannelFlagRequest request, CancellationToken cancellationToken)
        {
            var hasPermission = await _mediator.Send(new UserHasPermissionRequest(request.User, PermissionType.ManageFlags), cancellationToken);

            if (!hasPermission)
            {
                return ServiceResponse.Fail("Missing permission");
            }

            var hasExistingFlag = await _db.ChannelFlags
                .Where(x => x.DiscordChannelId == request.DiscordChannelId)
                .AnyAsync(x => x.Type == request.Flag, cancellationToken: cancellationToken);

            if (hasExistingFlag)
            {
                return ServiceResponse.Ok();
            }

            var entity = new ChannelFlag
            {
                DiscordChannelId = request.DiscordChannelId,
                Type = request.Flag,
            };

            _db.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetChannelsWithFlagRequest(request.Flag), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
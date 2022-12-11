using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags;

public sealed record DeleteChannelFlagRequest(PermissionUser User, ChannelFlagType Flag, ulong DiscordChannelId) : IRequest<ServiceResponse>;

[AutoConstructor]
public partial class DeleteChannelFlagHandler : IRequestHandler<DeleteChannelFlagRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public async Task<ServiceResponse> Handle(DeleteChannelFlagRequest request, CancellationToken cancellationToken)
    {
        var hasPermission = await _mediator.Send(new UserHasPermissionRequest(request.User, PermissionType.ManageFlags), cancellationToken);

        if (!hasPermission)
        {
            return ServiceResponse.Fail("Missing permission");
        }

        var flag = await _db.ChannelFlags
            .SingleAsync(x => x.DiscordChannelId == request.DiscordChannelId
                              && x.Type == request.Flag,
                cancellationToken: cancellationToken);

        _db.Remove(flag);

        await _db.SaveChangesAsync(cancellationToken);

        await _mediator.Send(new InvalidateGetChannelsWithFlagRequest(request.Flag), cancellationToken);

        return ServiceResponse.Ok();
    }
}
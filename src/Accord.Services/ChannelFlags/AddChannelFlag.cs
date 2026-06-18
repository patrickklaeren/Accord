using System.Linq;
using Accord.Domain.Model;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags;

public sealed record AddChannelFlagRequest(PermissionUser User, ChannelFlagType Flag, ulong DiscordChannelId) 
    : IRequest<ServiceResponse>;

public class AddChannelFlagHandler(UserPermissionService userPermissionService, 
    ChannelFlagService channelFlagService) : IRequestHandler<AddChannelFlagRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(AddChannelFlagRequest request, CancellationToken cancellationToken)
    {
        var hasPermission = await userPermissionService.HasPermission(request.User, PermissionType.ManageFlags);

        if (!hasPermission)
        {
            return ServiceResponse.Fail("Missing permission");
        }
        
        return await channelFlagService.AddChannelFlag(request.DiscordChannelId, request.Flag, cancellationToken);
    }
}
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags;

public sealed record DeleteChannelFlagRequest(PermissionUser User, ChannelFlagType Flag, ulong DiscordChannelId) : IRequest<ServiceResponse>;

public class DeleteChannelFlagHandler(ChannelFlagService channelFlagService,
    UserPermissionService userPermissionService) 
    : IRequestHandler<DeleteChannelFlagRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(DeleteChannelFlagRequest request, CancellationToken cancellationToken)
    {
        var hasPermission = await userPermissionService.HasPermission(request.User, PermissionType.ManageFlags);

        if (!hasPermission)
        {
            return ServiceResponse.Fail("Missing permission");
        }

        return await channelFlagService.DeleteChannelFlag(request.DiscordChannelId, request.Flag);
    }
}
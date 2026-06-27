using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.HelpForum;

public sealed record CanCloseHelpForumPostRequest(PermissionUser User, ulong DiscordChannelId) : IRequest<ServiceResponse>;

internal class CanCloseHelpForumPostHandler(ChannelFlagService channelFlagService, 
    UserPermissionService userPermissionService,
    IMediator mediator) 
    : IRequestHandler<CanCloseHelpForumPostRequest,ServiceResponse>
{
    public async Task<ServiceResponse> Handle(CanCloseHelpForumPostRequest request, CancellationToken cancellationToken)
    {
        var discordChannelResponse = await mediator.Send(new GetDiscordGuildChannelRequest(request.DiscordChannelId), cancellationToken);

        if (!discordChannelResponse.Success)
        {
            return ServiceResponse.Fail("Failed getting forum post");
        }

        var discordChannel = discordChannelResponse.Value!;
        
        if (!await userPermissionService.HasPermission(request.User, PermissionType.ForumHelper) 
            && request.User.DiscordUserId != discordChannel.OwnerDiscordUserId)
        {
            return ServiceResponse.Fail("Missing permission");
        }

        if (discordChannel.ParentDiscordChannelId is null 
            || !await channelFlagService.ChannelHasFlag(discordChannel.ParentDiscordChannelId.Value, ChannelFlagType.HelpForum, cancellationToken))
        {
            return ServiceResponse.Fail("Cannot close a channel that is not part of a help forum");
        }

        return ServiceResponse.Ok();
    }
}
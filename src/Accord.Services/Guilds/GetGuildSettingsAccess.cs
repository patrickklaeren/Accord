using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;

namespace Accord.Services.Guilds;

public sealed record GetGuildSettingsAccessRequest(ulong DiscordUserId) : IRequest<GuildSettingsAccessResponse>;

public sealed record GuildSettingsAccessResponse(bool CanManagePermissions, bool CanManageChannelFlags)
{
    public bool CanAccessSettings => CanManagePermissions || CanManageChannelFlags;
}

public sealed class GetGuildSettingsAccessHandler(IMediator mediator)
    : IRequestHandler<GetGuildSettingsAccessRequest, GuildSettingsAccessResponse>
{
    public async Task<GuildSettingsAccessResponse> Handle(GetGuildSettingsAccessRequest request, CancellationToken cancellationToken)
    {
        var access = await mediator.Send(new GetGuildAccessRequest(request.DiscordUserId), cancellationToken);
        if (access.User is not { } user)
            return new GuildSettingsAccessResponse(false, false);

        var canManageFlags = await mediator.Send(new UserHasPermissionRequest(user, PermissionType.ManageFlags), cancellationToken);
        return new GuildSettingsAccessResponse(user.IsAdministrator, canManageFlags);
    }
}

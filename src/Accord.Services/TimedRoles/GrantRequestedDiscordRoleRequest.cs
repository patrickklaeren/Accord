using MediatR;

namespace Accord.Services.TimedRoles;

public sealed record GrantRequestedDiscordRoleRequest(ulong DiscordUserId, ulong DiscordRoleId)
    : IRequest<ServiceResponse>;

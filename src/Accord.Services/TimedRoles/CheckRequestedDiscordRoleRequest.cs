using MediatR;

namespace Accord.Services.TimedRoles;

public sealed record CheckRequestedDiscordRoleRequest(ulong DiscordUserId, ulong DiscordRoleId)
    : IRequest<ServiceResponse>;

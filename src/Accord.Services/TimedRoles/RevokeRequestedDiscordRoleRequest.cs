using MediatR;

namespace Accord.Services.TimedRoles;

public sealed record RevokeRequestedDiscordRoleRequest(ulong DiscordUserId, ulong DiscordRoleId)
    : IRequest<bool>;

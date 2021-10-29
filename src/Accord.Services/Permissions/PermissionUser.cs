using System.Collections.Generic;

namespace Accord.Services.Permissions;

public record PermissionUser(ulong DiscordUserId, IEnumerable<ulong> OwnedDiscordRoleIds);
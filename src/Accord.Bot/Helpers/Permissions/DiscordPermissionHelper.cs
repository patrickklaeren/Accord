using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers.Permissions;

public class DiscordPermissionHelper
{
    private readonly DiscordCache _discordCache;

    public DiscordPermissionHelper(DiscordCache discordCache)
    {
        _discordCache = discordCache;
    }

    public bool HasBotEffectivePermissionsInChannel(ulong discordGuildId, IChannel discordChannelId, params DiscordPermission[] permissions)
    {
        var selfMember = _discordCache.GetGuildSelfMember(discordGuildId);
        return HasUserEffectivePermissionsInChannel(discordGuildId, selfMember, discordChannelId, permissions);
    }

    public bool HasUserEffectivePermissionsInChannel(ulong discordGuildId, IGuildMember guildMember, IChannel discordChannel, params DiscordPermission[] permissions)
    {
        var effectivePermissions = GetUserEffectivePermissionsInChannel(discordGuildId, guildMember, discordChannel);
            
        return permissions.Select(x => effectivePermissions.HasPermission(x)).All(x => x);
    }

    private IDiscordPermissionSet GetUserEffectivePermissionsInChannel(ulong discordGuildId, IGuildMember guildMember, IChannel discordChannel)
    {
        var guildEveryoneRole = _discordCache.GetEveryoneRole(discordGuildId);
        var guildRoles = _discordCache.GetGuildRoles(discordGuildId);
        var memberRoles = guildRoles.Where(x => guildMember.Roles.Contains(x.ID)).ToList();
        var channelPermissionOverwrites = discordChannel.PermissionOverwrites.Value;

        return DiscordPermissionSet.ComputePermissions(guildMember.User.Value!.ID, guildEveryoneRole, memberRoles, channelPermissionOverwrites);
    }
}

public static class DiscordPermissionHelperExtensions
{
    public static bool HasUserPermissionOverwrite(
        this IChannel channel,
        IUser user,
        DiscordPermission discordPermission,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => HasUserPermissionOverwrite(channel, user.ID.Value, discordPermission, discordPermissionType);

    private static bool HasUserPermissionOverwrite(
        this IChannel channel,
        ulong discordUserId,
        DiscordPermission discordPermission,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => channel.PermissionOverwrites.HasValue && channel.PermissionOverwrites.Value
        .Any(x => x.Type == PermissionOverwriteType.Member
                  && x.ID.Value == discordUserId
                  && (discordPermissionType == DiscordPermissionType.All && (x.Allow.HasPermission(discordPermission) || x.Deny.HasPermission(discordPermission))
                      || discordPermissionType == DiscordPermissionType.Allow && x.Allow.HasPermission(discordPermission)
                      || discordPermissionType == DiscordPermissionType.Deny && x.Deny.HasPermission(discordPermission)
                  ));
}
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers.Permissions;

public static class DiscordPermissionHelperExtensions
{
    public static bool HasUserPermissionOverwrite(
        this IChannel channel,
        IUser user,
        DiscordPermission discordPermission,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => HasUserPermissionOverwrite(channel, user.ID.Value, discordPermission, discordPermissionType);

    public static bool HasUserPermissionOverwrite(
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


    public static bool HasUserPermissionOverwrites(
        this IChannel channel,
        IUser user,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => HasUserPermissionOverwrites(channel, user.ID.Value, discordPermissionType);

    public static bool HasUserPermissionOverwrites(
        this IChannel channel,
        ulong discordUserId,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => channel.PermissionOverwrites.HasValue && channel.PermissionOverwrites.Value
        .Any(x => x.Type == PermissionOverwriteType.Member
                  && x.ID.Value == discordUserId
                  && (discordPermissionType == DiscordPermissionType.All && (x.Allow.Value != BigInteger.Zero || x.Deny.Value != BigInteger.Zero)
                      || discordPermissionType == DiscordPermissionType.Allow && x.Allow.Value != BigInteger.Zero
                      || discordPermissionType == DiscordPermissionType.Deny && x.Deny.Value != BigInteger.Zero
                  ));
}

public class DiscordPermissionHelper
{
    private readonly DiscordCache _discordCache;

    public DiscordPermissionHelper(DiscordCache discordCache)
    {
        _discordCache = discordCache;
    }

    public bool HasUserPermissionOverwriteInChannel(
        IChannel channel,
        ulong discordUserId,
        DiscordPermission discordPermission,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => channel.HasUserPermissionOverwrite(discordUserId, discordPermission, discordPermissionType);

    public bool HasUserPermissionOverwriteInChannel(
        IChannel channel,
        IUser user,
        DiscordPermission discordPermission,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => channel.HasUserPermissionOverwrite(user.ID.Value, discordPermission, discordPermissionType);

    public bool HasUserPermissionOverwritesInChannel(
        IChannel channel,
        ulong discordUserId,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => channel.HasUserPermissionOverwrites(discordUserId, discordPermissionType);

    public bool HasUserPermissionOverwritesInChannel(
        IChannel channel,
        IUser user,
        DiscordPermissionType discordPermissionType = DiscordPermissionType.All
    ) => channel.HasUserPermissionOverwrites(user.ID.Value, discordPermissionType);

    public async Task<bool> HasBotEffectivePermissionsInChannel(IChannel discordChannelId, params DiscordPermission[] permissions)
    {
        var selfMember = _discordCache.GetGuildSelfMember();

        return await HasUserEffectivePermissionsInChannel(selfMember, discordChannelId, permissions);
    }

    public async Task<bool> HasBotEffectivePermissionsInChannel(ulong discordChannelId, params DiscordPermission[] permissions)
    {
        var guildChannel = (await _discordCache.GetGuildChannels()).SingleOrDefault(x => x.ID.Value == discordChannelId);

        if (guildChannel == null)
            return false;

        return await HasBotEffectivePermissionsInChannel(guildChannel, permissions);
    }

    public async Task<IDiscordPermissionSet?> GetBotEffectivePermissionsInChannel(ulong discordChannelId)
    {
        var guildChannel = (await _discordCache.GetGuildChannels()).SingleOrDefault(x => x.ID.Value == discordChannelId);

        if (guildChannel is null)
            return null;

        return await GetBotEffectivePermissionsInChannel(guildChannel);
    }

    public async Task<IDiscordPermissionSet> GetBotEffectivePermissionsInChannel(IChannel discordChannel)
    {
        var selfMember = _discordCache.GetGuildSelfMember();
        return await GetUserEffectivePermissionsInChannel(selfMember, discordChannel);
    }

    public async Task<bool> HasUserEffectivePermissionsInChannel(IGuildMember guildMember, IChannel discordChannel, params DiscordPermission[] permissions)
    {
        var effectivePermissions = await GetUserEffectivePermissionsInChannel(guildMember, discordChannel);
        return permissions.Select(x => effectivePermissions.HasPermission(x)).All(x => x);
    }

    public async Task<bool> HasUserEffectivePermissionsInChannel(ulong discordUserId, ulong discordChannelId, params DiscordPermission[] permissions)
    {
        var effectivePermissions = await GetUserEffectivePermissionsInChannel(discordUserId, discordChannelId);

        if (effectivePermissions is null)
            return false;

        return permissions.Select(x => effectivePermissions.HasPermission(x)).All(x => x);
    }

    public async Task<IDiscordPermissionSet> GetUserEffectivePermissionsInChannel(IGuildMember guildMember, IChannel discordChannel)
    {
        var guildEveryoneRole = _discordCache.GetEveryoneRole();
        var guildRoles = await _discordCache.GetGuildRoles();
        var memberRoles = guildRoles.Where(x => guildMember.Roles.Contains(x.ID)).ToList();
        var channelPermissionOverwrites = discordChannel.PermissionOverwrites.Value;

        return DiscordPermissionSet.ComputePermissions(guildMember.User.Value!.ID, guildEveryoneRole, memberRoles, channelPermissionOverwrites);
    }

    public async Task<IDiscordPermissionSet?> GetUserEffectivePermissionsInChannel(ulong discordUserId, ulong discordChannelId)
    {
        var guildMember = await _discordCache.GetGuildMember(discordUserId);

        if (!guildMember.IsSuccess)
            return null;

        var guildChannel = (await _discordCache.GetGuildChannels()).SingleOrDefault(x => x.ID.Value == discordChannelId);

        if (guildChannel is null)
            return null;

        return await GetUserEffectivePermissionsInChannel(guildMember.Entity, guildChannel);
    }
}
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers.Permissions
{
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

        public bool HasBotEffectivePermissionsInChannel(ulong discordGuildId, IChannel discordChannelId, params DiscordPermission[] permissions)
        {
            var selfMember = _discordCache.GetGuildSelfMember(discordGuildId);

            return HasUserEffectivePermissionsInChannel(discordGuildId, selfMember, discordChannelId, permissions);
        }

        public bool HasBotEffectivePermissionsInChannel(ulong discordGuildId, ulong discordChannelId, params DiscordPermission[] permissions)
        {
            var guildChannel = _discordCache.GetGuildChannels(discordChannelId).SingleOrDefault(x => x.ID.Value == discordChannelId);
            if (guildChannel == null)
                return false;

            return HasBotEffectivePermissionsInChannel(discordGuildId, guildChannel, permissions);
        }

        public IDiscordPermissionSet? GetBotEffectivePermissionsInChannel(ulong discordGuildId, ulong discordChannelId)
        {
            var guildChannel = _discordCache.GetGuildChannels(discordChannelId).SingleOrDefault(x => x.ID.Value == discordChannelId);
            if (guildChannel == null)
                return null;

            return GetBotEffectivePermissionsInChannel(discordGuildId, guildChannel);
        }

        public IDiscordPermissionSet GetBotEffectivePermissionsInChannel(ulong discordGuildId, IChannel discordChannel)
        {
            var selfMember = _discordCache.GetGuildSelfMember(discordGuildId);
            return GetUserEffectivePermissionsInChannel(discordGuildId, selfMember, discordChannel);
        }

        public bool HasUserEffectivePermissionsInChannel(ulong discordGuildId, IGuildMember guildMember, IChannel discordChannel, params DiscordPermission[] permissions)
        {
            var effectivePermissions = GetUserEffectivePermissionsInChannel(discordGuildId, guildMember, discordChannel);
            
            return permissions.Select(x => effectivePermissions.HasPermission(x)).All(x => x);
        }

        public async Task<bool> HasUserEffectivePermissionsInChannel(ulong discordGuildId, ulong discordUserId, ulong discordChannelId, params DiscordPermission[] permissions)
        {
            var effectivePermissions = await GetUserEffectivePermissionsInChannel(discordGuildId, discordUserId, discordChannelId);
            if (effectivePermissions == null)
                return false;

            return permissions.Select(x => effectivePermissions.HasPermission(x)).All(x => x);
        }

        public IDiscordPermissionSet GetUserEffectivePermissionsInChannel(ulong discordGuildId, IGuildMember guildMember, IChannel discordChannel)
        {
            var guildEveryoneRole = _discordCache.GetEveryoneRole(discordGuildId);
            var guildRoles = _discordCache.GetGuildRoles(discordGuildId);
            var memberRoles = guildRoles.Where(x => guildMember.Roles.Contains(x.ID)).ToList();
            var channelPermissionOverwrites = discordChannel.PermissionOverwrites.Value;

            return DiscordPermissionSet.ComputePermissions(guildMember.User.Value!.ID, guildEveryoneRole, memberRoles, channelPermissionOverwrites);
        }

        public async Task<IDiscordPermissionSet?> GetUserEffectivePermissionsInChannel(ulong discordGuildId, ulong discordUserId, ulong discordChannelId)
        {
            var guildMember = await _discordCache.GetGuildMember(discordGuildId, discordUserId);
            if (!guildMember.IsSuccess)
                return null;

            var guildChannel = _discordCache.GetGuildChannels(discordChannelId).SingleOrDefault(x => x.ID.Value == discordChannelId);
            if (guildChannel == null)
                return null;

            return GetUserEffectivePermissionsInChannel(discordGuildId, guildMember.Entity, guildChannel);
        }
    }
}
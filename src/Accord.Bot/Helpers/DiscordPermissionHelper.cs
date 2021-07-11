using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers
{
    public enum DiscordPermissionType
    {
        Allow,
        Deny,
        All
    }

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

        public bool HasBotEffectivePermissionInChannel(ulong discordGuildId, IChannel discordChannelId, DiscordPermission permission)
        {
            var selfMember = _discordCache.GetGuildSelfMember(discordGuildId);

            return HasUserEffectivePermissionInChannel(discordGuildId, selfMember, discordChannelId, permission);
        }

        public bool HasBotEffectivePermissionInChannel(ulong discordGuildId, ulong discordChannelId, DiscordPermission permission)
        {
            var guildChannel = _discordCache.GetGuildChannels(discordChannelId).SingleOrDefault(x => x.ID.Value == discordChannelId);
            if (guildChannel == null)
                return false;

            return HasBotEffectivePermissionInChannel(discordGuildId, guildChannel, permission);
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

        public bool HasUserEffectivePermissionInChannel(ulong discordGuildId, IGuildMember guildMember, IChannel discordChannel, DiscordPermission permission)
        {
            var effectivePermissions = GetUserEffectivePermissionsInChannel(discordGuildId, guildMember, discordChannel);
            return effectivePermissions.HasPermission(permission);
        }

        public async Task<bool> HasUserEffectivePermissionInChannel(ulong discordGuildId, ulong discordUserId, ulong discordChannelId, DiscordPermission permission)
        {
            var effectivePermissions = await GetUserEffectivePermissionsInChannel(discordGuildId, discordUserId, discordChannelId);
            if (effectivePermissions == null)
                return false;

            return effectivePermissions.HasPermission(permission);
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
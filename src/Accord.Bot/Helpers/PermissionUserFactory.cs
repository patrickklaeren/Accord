using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.Permissions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class PermissionUserFactory(IDiscordRestGuildAPI guildApi,
    DiscordCache discordCache,
    DiscordConfiguration discordConfiguration)
{
    public async Task<PermissionUser> FromId(ulong discordUserId)
    {
        var selfUserSnowflake = discordCache.GetSelfSnowflake();
        var guildSnowflake = new Snowflake(discordConfiguration.GuildId);
        var userSnowflake = new Snowflake(discordUserId);
        
        var isSelfUser = selfUserSnowflake == userSnowflake;
        
        var guild = await guildApi.GetGuildAsync(guildSnowflake);

        if (!guild.IsSuccess)
        {
            throw new InvalidOperationException("Cannot get guild");
        }

        var member = await guildApi.GetGuildMemberAsync(guildSnowflake, userSnowflake);

        if (member.Entity is null)
        {
            throw new InvalidOperationException("Cannot get user when they do not exist in guild");
        }

        var isAdministrator = guild.Entity.OwnerID == userSnowflake;

        if (!isAdministrator)
        {
            var memberRoleIds = member.Entity.Roles.Select(x => x.Value).ToHashSet();
            var guildRoles = await guildApi.GetGuildRolesAsync(guildSnowflake);

            isAdministrator = guildRoles.Entity
                .Where(r => memberRoleIds.Contains(r.ID.Value))
                .Any(r => r.Permissions.HasPermission(DiscordPermission.Administrator));
        }

        var roles = member.Entity.Roles.Select(x => x.Value);

        return new PermissionUser(discordUserId, roles, isAdministrator, isSelfUser);
    }
}
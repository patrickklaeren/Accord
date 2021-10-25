using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;

namespace Accord.Bot.Helpers;

public static class CommandContextExtensions
{
    public static async Task<PermissionUser> ToPermissionUser(this ICommandContext context, IDiscordRestGuildAPI guildApi)
    {
        if (!context.GuildID.HasValue)
        {
            throw new InvalidOperationException("Cannot get user from non Guild context");
        }

        var member = await guildApi.GetGuildMemberAsync(context.GuildID.Value, context.User.ID);

        if (member.Entity is null)
        {
            throw new InvalidOperationException("Cannot get user when they do not exist in guild");
        }

        var roles = member.Entity.Roles.Select(x => x.Value);

        return new PermissionUser(context.User.ID.Value, roles);
    }
}
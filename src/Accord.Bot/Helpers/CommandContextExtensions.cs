using System;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Infrastructure;
using Accord.Services.Permissions;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace Accord.Bot.Helpers;

public static class CommandContextExtensions
{
    public static async Task<PermissionUser> ToPermissionUser(this ICommandContext context, IDiscordRestGuildAPI guildApi)
    {
        var proxy = context.GetCommandProxy();
        
        var member = await guildApi.GetGuildMemberAsync(proxy.GuildId, proxy.UserId);

        if (member.Entity is null)
        {
            throw new InvalidOperationException("Cannot get user when they do not exist in guild");
        }

        var roles = member.Entity.Roles.Select(x => x.Value);

        return new PermissionUser(proxy.UserId.Value, roles);
    }
    
    public static CommandContextProxy GetCommandProxy(this ICommandContext context)
    {
        return context switch
        {
            IInteractionCommandContext ix => new CommandContextProxy(ix.Interaction.Member.Value.User.Value.ID, ix.Interaction.GuildID.Value, ix.Interaction.ChannelID.Value),
            ITextCommandContext tx => new CommandContextProxy(tx.Message.Author.Value.ID, tx.GuildID.Value, tx.Message.ChannelID.Value),
            _ => throw new NotSupportedException()
        };
    }

    public record CommandContextProxy(Snowflake UserId, Snowflake GuildId, Snowflake ChannelId);
}
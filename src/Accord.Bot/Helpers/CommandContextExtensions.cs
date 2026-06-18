using System;
using System.Threading.Tasks;
using Accord.Services.Permissions;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace Accord.Bot.Helpers;

public static class CommandContextExtensions
{
    extension(ICommandContext context)
    {
        public async Task<PermissionUser> ToPermissionUser(PermissionUserFactory permissionUserFactory)
        {
            var proxy = context.GetCommandProxy();
            return await permissionUserFactory.FromId(proxy.UserId.Value);
        }

        public CommandContextProxy GetCommandProxy()
        {
            return context switch
            {
                IInteractionCommandContext ix => new CommandContextProxy(ix.Interaction.Member.Value.User.Value.ID, ix.Interaction.GuildID.Value, ix.Interaction.Channel.Value.ID.Value),
                ITextCommandContext tx => new CommandContextProxy(tx.Message.Author.Value.ID, tx.GuildID.Value, tx.Message.ChannelID.Value),
                _ => throw new NotSupportedException()
            };
        }
    }

    public record CommandContextProxy(Snowflake UserId, Snowflake GuildId, Snowflake ChannelId);
}
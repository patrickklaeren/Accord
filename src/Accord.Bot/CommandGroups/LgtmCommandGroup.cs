using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Xp;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    [Group("lgtm")]
    public class LgtmCommandGroup : CommandGroup
    {
        private readonly CommandResponder _commandResponder;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly ICommandContext _commandContext;

        private const string ROLE_NAME = "LGTM";

        public LgtmCommandGroup(CommandResponder commandResponder, IDiscordRestGuildAPI guildApi, ICommandContext commandContext)
        {
            _commandResponder = commandResponder;
            _guildApi = guildApi;
            _commandContext = commandContext;
        }

        [RequireContext(ChannelContext.Guild), Command("subscribe"), Description("Subscribe to LGTM role")]
        public async Task<IResult> Subscribe()
        {
            var roles = await _guildApi.GetGuildRolesAsync(_commandContext.GuildID.Value);
            
            if (roles.IsSuccess && roles.Entity.Any(x => x.Name == ROLE_NAME))
            {
                var role = roles.Entity.Single(x => x.Name == ROLE_NAME);
                await _guildApi.AddGuildMemberRoleAsync(_commandContext.GuildID.Value, _commandContext.User.ID, role.ID);
            }
            
            return await _commandResponder.Respond("Subscribed!");
        }

        [RequireContext(ChannelContext.Guild), RequireUserGuildPermission(DiscordPermission.Administrator), Command("unsubscribe"), Description("Unsubscribe from LGTM role")]
        public async Task<IResult> Unsubscribe()
        {
            var roles = await _guildApi.GetGuildRolesAsync(_commandContext.GuildID.Value);
            
            if (roles.IsSuccess && roles.Entity.Any(x => x.Name == ROLE_NAME))
            {
                var role = roles.Entity.Single(x => x.Name == ROLE_NAME);
                await _guildApi.RemoveGuildMemberRoleAsync(_commandContext.GuildID.Value, _commandContext.User.ID, role.ID);
            }
            
            return await _commandResponder.Respond("Unsubscribed!");
        }
    }
}

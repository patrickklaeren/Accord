using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("lgtm"), AutoConstructor]
public partial class LgtmCommandGroup: AccordCommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly ICommandContext _commandContext;

    private const string ROLE_NAME = "LGTM";

    [Command("subscribe"), Description("Subscribe to LGTM role"), Ephemeral]
    public async Task<IResult> Subscribe()
    {
        var proxy = _commandContext.GetCommandProxy();
        var roles = await _guildApi.GetGuildRolesAsync(proxy.GuildId);
            
        if (roles.IsSuccess && roles.Entity.Any(x => x.Name == ROLE_NAME))
        {
            var role = roles.Entity.Single(x => x.Name == ROLE_NAME);
            await _guildApi.AddGuildMemberRoleAsync(proxy.GuildId, proxy.UserId, role.ID);
        }
        
        return await _feedbackService.SendContextualAsync("Subscribed!");
    }

    [Command("unsubscribe"), Description("Unsubscribe from LGTM role")]
    public async Task<IResult> Unsubscribe()
    {
        var proxy = _commandContext.GetCommandProxy();
        var roles = await _guildApi.GetGuildRolesAsync(proxy.GuildId);
            
        if (roles.IsSuccess && roles.Entity.Any(x => x.Name == ROLE_NAME))
        {
            var role = roles.Entity.Single(x => x.Name == ROLE_NAME);
            await _guildApi.RemoveGuildMemberRoleAsync(proxy.GuildId, proxy.UserId, role.ID);
        }
            
        return await _feedbackService.SendContextualAsync("Unsubscribed!");
    }
}
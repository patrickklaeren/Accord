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

// TODO: This needs to be enabled after more extensive testing

[Group("nitro"), AutoConstructor]
public partial class NitroBoosterCommandGroup : AccordCommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly ICommandContext _commandContext;

    private const string NITRO_ROLE = "Nitro Booster";
    private const string RANKED_NITRO_ROLE = "Ranked Nitro Booster";

    [Command("apply"), Description("Applies Ranked Nitro Booster role to your profile so you become all pink and special"), Ephemeral]
    public async Task<IResult> Apply()
    {
        var proxy = _commandContext.GetCommandProxy();
        
        var roles = await _guildApi.GetGuildRolesAsync(proxy.GuildId);
        
        if (roles.IsSuccess && roles.Entity.Any(x => x.Name == RANKED_NITRO_ROLE))
        {
            var role = roles.Entity.Single(x => x.Name == RANKED_NITRO_ROLE);
            await _guildApi.AddGuildMemberRoleAsync(proxy.GuildId, proxy.UserId, role.ID);
        }
        
        return await _feedbackService.SendContextualAsync("Applied!");
    }

    [Command("remove"), Description("Removes Ranked Nitro Booster role from your profile so you go back to your default role colour"), Ephemeral]
    public async Task<IResult> Remove()
    {
        var proxy = _commandContext.GetCommandProxy();
        var roles = await _guildApi.GetGuildRolesAsync(proxy.GuildId);
            
        if (roles.IsSuccess && roles.Entity.Any(x => x.Name == RANKED_NITRO_ROLE))
        {
            var role = roles.Entity.Single(x => x.Name == RANKED_NITRO_ROLE);
            await _guildApi.RemoveGuildMemberRoleAsync(proxy.GuildId, proxy.UserId, role.ID);
        }
        
        return await _feedbackService.SendContextualAsync("Removed!");
    }
}
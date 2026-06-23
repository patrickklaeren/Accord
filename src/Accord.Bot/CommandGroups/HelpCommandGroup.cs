using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Services;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class HelpCommandGroup(AccordConfiguration accordConfiguration, 
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("help"), Description("Get help using the bot"), Ephemeral]
    public async Task<IResult> Help()
    {
        var url = accordConfiguration.AppBaseUrl.EndsWith("/") 
            ? accordConfiguration.AppBaseUrl 
            : accordConfiguration.AppBaseUrl + "/";
        
        var helpUrl = url + "help";
        
        return await feedbackService.SendContextualAsync($"Get help and see more at {helpUrl}");
    }
}
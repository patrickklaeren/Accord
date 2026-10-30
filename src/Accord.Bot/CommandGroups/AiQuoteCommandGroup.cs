using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class AiQuoteCommandGroup(FeedbackService feedbackService, IMediator mediator) : AccordCommandGroup
{
    [Command("ai"), Description("Get a random AI pushback quote")]
    public async Task<IResult> AiQuote()
    {
        var response = await mediator.Send(new GetAiQuoteRequest());
        return await feedbackService.SendContextualAsync(response.Quote);
    }
}

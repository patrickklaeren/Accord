using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace Accord.Bot.Infrastructure;

[AutoConstructor]
public partial class AfterCommandPostExecutionEvent : IPostExecutionEvent
{
    private readonly FeedbackService _feedbackService;

    private const string DEFAULT_ERROR_MESSAGE = "Something went wrong, there is no message for this, help me out by submitting a useful message via my repo!";
    private const string NO_MATCHING_COMMAND_FOUND = "No matching command could be found.";

    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = new CancellationToken())
    {
        if (!commandResult.IsSuccess)
        {
            var responseMessage = commandResult.Error?.Message ?? DEFAULT_ERROR_MESSAGE;

            if (responseMessage != NO_MATCHING_COMMAND_FOUND)
            {
                await _feedbackService.SendContextualAsync(responseMessage, ct: ct);
            }
        }

        return Result.FromSuccess();
    }
}
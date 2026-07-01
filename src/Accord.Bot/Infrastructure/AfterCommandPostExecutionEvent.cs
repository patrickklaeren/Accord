using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Results;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace Accord.Bot.Infrastructure;

public class AfterCommandPostExecutionEvent(FeedbackService feedbackService) : IPostExecutionEvent
{
    private const string DEFAULT_ERROR_MESSAGE = "Something went wrong. Try again or report this as a bug!";
    private const string NO_MATCHING_COMMAND_FOUND = "No matching command could be found.";

    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = new CancellationToken())
    {
        if (!commandResult.IsSuccess)
        {
            var responseMessage = commandResult.Error is ConditionNotSatisfiedError
                ? "You do not have permission to use this command."
                : commandResult.Error?.Message ?? DEFAULT_ERROR_MESSAGE;

            if (responseMessage != NO_MATCHING_COMMAND_FOUND)
            {
                await feedbackService.SendContextualAsync(responseMessage, ct: ct);
            }
        }

        return Result.FromSuccess();
    }
}

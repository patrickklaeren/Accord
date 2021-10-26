using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace Accord.Bot.Infrastructure;

public class AfterCommandPostExecutionEvent : IPostExecutionEvent
{
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IDiscordRestChannelAPI _channelApi;

    public AfterCommandPostExecutionEvent(IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi)
    {
        _interactionApi = interactionApi;
        _channelApi = channelApi;
    }

    private const string DEFAULT_ERROR_MESSAGE = "Something went wrong, there is no message for this, help me out by submitting a useful message via my repo!";
    private const string NO_MATCHING_COMMAND_FOUND = "No matching command could be found.";

    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = new CancellationToken())
    {
        if (!commandResult.IsSuccess)
        {
            var responseMessage = commandResult.Error?.Message ?? DEFAULT_ERROR_MESSAGE;

            if (responseMessage == NO_MATCHING_COMMAND_FOUND)
            {
                // We do not care if it is not a matching command, leave it and
                // continue
            }
            else if (context is InteractionContext interactionContext)
            {
                await _interactionApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, content: responseMessage, ct: ct);
            }
            else
            {
                await _channelApi.CreateMessageAsync(context.ChannelID, content: responseMessage, ct: ct);
            }

            return Result.FromSuccess();
        }

        return Result.FromSuccess();
    }
}
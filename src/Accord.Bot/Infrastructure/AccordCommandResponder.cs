using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace Accord.Bot.Infrastructure
{
    public class AfterCommandPostExecutionEvent : IPostExecutionEvent
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public AfterCommandPostExecutionEvent(IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi)
        {
            _interactionApi = interactionApi;
            _channelApi = channelApi;
        }

        public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = new CancellationToken())
        {
            if (!commandResult.IsSuccess)
            {
                var responseMessage = commandResult.Error?.Message 
                                      ?? "Something went wrong, there is no message for this, help me out by submitting a useful message via my repo!";
                
                if (context is InteractionContext interactionContext)
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
}
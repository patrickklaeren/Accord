using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.Helpers
{
    public class CommandResponder
    {
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public CommandResponder(IDiscordRestWebhookAPI webhookApi, IDiscordRestChannelAPI channelApi, ICommandContext commandContext)
        {
            _webhookApi = webhookApi;
            _channelApi = channelApi;
            _commandContext = commandContext;
        }

        public async Task<IResult> Respond(string message)
        {
            if (_commandContext is InteractionContext interactionContext)
            {
                return await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, content: message);
            }

            return await _channelApi.CreateMessageAsync(_commandContext.ChannelID, content: message);
        }

        public async Task<IResult> Respond(Embed embed)
        {
            if (_commandContext is InteractionContext interactionContext)
            {
                return await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, embeds: new[] { embed });
            }

            return await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embed: embed);
        }

        public async Task<IResult> Respond(params Embed[] embeds)
        {
            if (_commandContext is InteractionContext interactionContext)
            {
                return await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, embeds: embeds);
            }

            foreach (var embed in embeds)
            {
                var response = await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embed: embed);

                if (!response.IsSuccess)
                    return response;
            }

            return Result.FromSuccess();
        }
    }
}

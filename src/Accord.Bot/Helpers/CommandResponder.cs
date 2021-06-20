using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;

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

        public async Task Respond(string message)
        {
            if (_commandContext is InteractionContext interactionContext)
            {
                await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, content: message);
            }
            else
            {
                await _channelApi.CreateMessageAsync(_commandContext.ChannelID, content: message);
            }
        }

        public async Task Respond(Embed embed)
        {
            if (_commandContext is InteractionContext interactionContext)
            {
                await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, embeds: new[] { embed });
            }
            else
            {
                await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embed: embed);
            }
        }
    }
}

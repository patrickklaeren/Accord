using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    public class ChannelFlagCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly ChannelFlagService _channelFlagService;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public ChannelFlagCommandGroup(ICommandContext commandContext, 
            ChannelFlagService channelFlagService, 
            IDiscordRestWebhookAPI webhookApi, 
            IDiscordRestChannelAPI channelApi)
        {
            _commandContext = commandContext;
            _channelFlagService = channelFlagService;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
        }

        [Command("addflag"), Description("Add flag to the current channel")]
        public async Task<IResult> AddFlag(string type)
        {
            var isValidFlag = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

            if (!isValidFlag)
            {
                await Respond("Type of flag is not found");
            }
            else
            {
                await _channelFlagService.AddFlag(actualChannelFlag, _commandContext.ChannelID.Value);
                await Respond($"{actualChannelFlag} flag added");
            }

            return Result.FromSuccess();
        }

        private async Task Respond(string message)
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
    }
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    public class RunOptionCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly RunOptionService _runOptionService;

        public RunOptionCommandGroup(ICommandContext commandContext, 
            IDiscordRestWebhookAPI webhookApi, 
            IDiscordRestChannelAPI channelApi,
            RunOptionService runOptionService)
        {
            _commandContext = commandContext;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
            _runOptionService = runOptionService;
        }

        [RequireContext(ChannelContext.Guild), RequireUserGuildPermission(DiscordPermission.Administrator), Command("configure"), Description("Configure an option for the bot")]
        public async Task<IResult> Configure(string type, string value)
        {
            if (!Enum.TryParse<RunOptionType>(type, out var actualRunOptionType) || !Enum.IsDefined(actualRunOptionType))
            {
                await Respond("Configuration is not found");
            }
            else
            {
                var response = await _runOptionService.Update(actualRunOptionType, value);

                if (response.Success)
                {
                    await Respond($"{actualRunOptionType} configuration updated to {value}");
                }
                else
                {
                    await Respond($"{response.ErrorMessage}");
                }
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

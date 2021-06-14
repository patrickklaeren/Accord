using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
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
    public class ChannelFlagCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly ChannelFlagService _channelFlagService;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;

        public ChannelFlagCommandGroup(ICommandContext commandContext, 
            ChannelFlagService channelFlagService, 
            IDiscordRestWebhookAPI webhookApi, 
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi)
        {
            _commandContext = commandContext;
            _channelFlagService = channelFlagService;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
            _guildApi = guildApi;
        }

        [RequireContext(ChannelContext.Guild), Command("addflag"), Description("Add flag to the current channel")]
        public async Task<IResult> AddFlag(string type)
        {
            var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

            if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
            {
                await Respond("Type of flag is not found");
            }
            else
            {
                var user = await _commandContext.ToPermissionUser(_guildApi);

                var response = await _channelFlagService.AddFlag(user, actualChannelFlag, _commandContext.ChannelID.Value);

                await response.GetAction(async () => await Respond($"{actualChannelFlag} flag added"),
                    async () => await Respond(response.ErrorMessage));
            }

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("removeflag"), Description("Add flag to the current channel")]
        public async Task<IResult> RemoveFlag(string type)
        {
            var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

            if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
            {
                await Respond("Type of flag is not found");
            }
            else
            {
                var user = await _commandContext.ToPermissionUser(_guildApi);

                var response = await _channelFlagService.DeleteFlag(user, actualChannelFlag, _commandContext.ChannelID.Value);

                await response.GetAction(async () => await Respond($"{actualChannelFlag} flag removed"),
                    async () => await Respond(response.ErrorMessage));
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

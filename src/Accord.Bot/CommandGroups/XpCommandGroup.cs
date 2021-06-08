using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    public class XpCommandGroup : CommandGroup
    {
        private readonly XpService _xpService;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestWebhookAPI _webhookApi;

        public XpCommandGroup(XpService xpService, ICommandContext commandContext, 
            IDiscordRestChannelAPI channelApi, IDiscordRestWebhookAPI webhookApi)
        {
            _xpService = xpService;
            _commandContext = commandContext;
            _channelApi = channelApi;
            _webhookApi = webhookApi;
        }

        [Command("leaderboard"), Description("Get a leaderboard of XP")]
        public async Task<IResult> GetLeaderboard()
        {
            var leaderboard = await _xpService.GetLeaderboard();

            var payload = string.Join(Environment.NewLine, leaderboard
                .Select(x => $"{DiscordMentionHelper.IdToMention(x.DiscordUserId)} {x.Xp}"));

            var embed = new Embed(Title: "Leaderboard", Description: payload);

            if (_commandContext is InteractionContext interactionContext)
            {
                await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, embeds: new[] {embed});
            }
            else
            {
                await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embed: embed);
            }

            return Result.FromSuccess();
        }
    }
}

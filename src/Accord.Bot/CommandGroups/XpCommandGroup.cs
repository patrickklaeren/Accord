using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
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

        public XpCommandGroup(XpService xpService, ICommandContext commandContext, IDiscordRestChannelAPI channelApi)
        {
            _xpService = xpService;
            _commandContext = commandContext;
            _channelApi = channelApi;
        }

        [Command("leaderboard"), Description("Get a leaderboard of XP")]
        public async Task<IResult> GetLeaderboard()
        {
            var leaderboard = await _xpService.GetLeaderboard();

            var payload = string.Join(Environment.NewLine, leaderboard.Select(x => $"<@{x.DiscordUserId}> {x.Xp}"));

            var embed = new Embed(Title: "Leaderboard", Description: payload);

            await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embed: embed);

            return Result.FromSuccess();
        }
    }
}

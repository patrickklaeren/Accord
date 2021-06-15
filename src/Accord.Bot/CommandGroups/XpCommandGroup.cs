﻿using System;
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
using Remora.Discord.Commands.Conditions;
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

        [RequireContext(ChannelContext.Guild), Command("leaderboard"), Description("Get a leaderboard of XP")]
        public async Task<IResult> GetLeaderboard()
        {
            var leaderboard = await _xpService.GetLeaderboard();

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("**Messages XP**");

            var messageUsers = string.Join(Environment.NewLine, leaderboard.MessageUsers
                .Select(x => $"{DiscordMentionHelper.IdToMention(x.DiscordUserId)} {x.Xp}"));

            stringBuilder.Append(messageUsers);

            stringBuilder.AppendLine(string.Empty);

            stringBuilder.AppendLine("**Voice Minutes**");

            var voiceUsers = string.Join(Environment.NewLine, leaderboard.VoiceUsers
                .Select(x => $"{DiscordMentionHelper.IdToMention(x.DiscordUserId)} {x.MinutesInVoiceChannel}"));

            stringBuilder.Append(voiceUsers);

            var embed = new Embed(Title: "Leaderboard", Description: stringBuilder.ToString());

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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Services.Extensions;
using Accord.Services.Users;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    [Group("moderation")]
    public class ModerationCommandGroup : CommandGroup
    {
        private readonly IMediator _mediator;
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly CommandResponder _commandResponder;

        public ModerationCommandGroup(IMediator mediator, ICommandContext commandContext,
            IDiscordRestGuildAPI guildApi,
            CommandResponder commandResponder)
        {
            _mediator = mediator;
            _commandContext = commandContext;
            _guildApi = guildApi;
            _commandResponder = commandResponder;
        }

        [RequireContext(ChannelContext.Guild), Command("risky-users"), Description("Get risky Guild users")]
        public async Task<IResult> GetRiskyUsers()
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var response = await _mediator.Send(new GetRiskyUsersRequest(user));

            if (!response.Success || response.Value is null)
            {
                await _commandResponder.Respond(response.ErrorMessage);
                return Result.FromSuccess();
            }

            var batches = response.Value.Batch(10);

            var embeds = new List<Embed>();

            foreach (var batch in batches)
            {
                var payload = string.Join($"{Environment.NewLine}",
                    batch.Select((x, i) => new StringBuilder()
                        .AppendLine($"**{x.UsernameWithDiscriminator}**")
                        .AppendLine($"Profile: {DiscordMentionHelper.UserIdToMention(x.DiscordUserId)}")
                        .AppendLine($"Joined: {x.JoinedDateTime:yyyy-MM-dd HH:mm:ss}")
                        .AppendLine($"Created: {x.CreatedDateTime:yyyy-MM-dd HH:mm:ss}")
                        .ToString()));

                embeds.Add(new Embed(Title: "Risky Users", Description: payload));
            }

            return await _commandResponder.Respond(embeds.ToArray());
        }
    }
}

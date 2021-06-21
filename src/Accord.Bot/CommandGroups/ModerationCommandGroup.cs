using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.Users;
using Humanizer;
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

            var payload = string.Join($"{Environment.NewLine}{Environment.NewLine}", 
                response.Value.Select((x, i) => 
                    $"**User {i}**" +
                    $"ID: {x.DiscordUserId}" +
                    $"Profile: {DiscordMentionHelper.UserIdToMention(x.DiscordUserId)}" +
                    $"Handle: {x.UsernameWithDiscriminator}" +
                    $"Joined: {x.JoinedDateTime:yyyy-MM-dd HH:mm:ss}" +
                    $"Created: {x.CreatedDateTime:yyyy-MM-dd HH:mm:ss}"));

            var embed = new Embed(Title: "Risky Users",
                Description: payload);

            await _commandResponder.Respond(embed);

            return Result.FromSuccess();
        }
    }
}

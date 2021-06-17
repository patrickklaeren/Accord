using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.NamePatterns;
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
    [Group("name-pattern")]
    public class NamePatternCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;

        public NamePatternCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestWebhookAPI webhookApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
            _guildApi = guildApi;
        }

        [RequireContext(ChannelContext.Guild), Command("list"), Description("List all name patterns")]
        public async Task<IResult> List()
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var response = await _mediator.Send(new GetNamePatternsRequest());

            var blocked = response.Any(x => x.Type == PatternType.Blocked)
                ? string.Join(Environment.NewLine, response.Where(x => x.Type == PatternType.Blocked).Select(x => $"- `{x.Pattern}` [{x.OnDiscovery}]"))
                : "There are no blocked patterns";

            var allowed = response.Any(x => x.Type == PatternType.Allowed)
                ? string.Join(Environment.NewLine, response.Where(x => x.Type == PatternType.Allowed).Select(x => $"- `{x.Pattern}`"))
                : "There are no allowed patterns";

            var embed = new Embed(Title: "Name patterns",
                Description: "Allowed patterns supersede those that are blocked.",
                Fields: new EmbedField[]
                {
                    new("Blocked", blocked),
                    new("Allowed", allowed),
                });

            if (_commandContext is InteractionContext interactionContext)
            {
                await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, embeds: new[] { embed });
            }
            else
            {
                await _channelApi.CreateMessageAsync(_commandContext.ChannelID, embed: embed);
            }

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("allow"), Description("Add name pattern to allow")]
        public async Task<IResult> AllowPattern(string pattern)
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var response = await _mediator.Send(new AddNamePatternRequest(user, pattern, PatternType.Allowed, OnNamePatternDiscovery.DoNothing));

            await response.GetAction(async () => await Respond($"{pattern} Allowed"),
                async () => await Respond(response.ErrorMessage));

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("block"), Description("Add name pattern to block")]
        public async Task<IResult> BlockPattern(string pattern, string onDiscovery)
        {
            var isParsedOnDiscovery = Enum.TryParse<OnNamePatternDiscovery>(onDiscovery, out var actualOnDiscovery);

            if (!isParsedOnDiscovery || !Enum.IsDefined(actualOnDiscovery))
            {
                await Respond("Pattern discovery is not found");
            }
            else
            {
                var user = await _commandContext.ToPermissionUser(_guildApi);

                var response = await _mediator.Send(new AddNamePatternRequest(user, pattern, PatternType.Blocked, actualOnDiscovery));

                await response.GetAction(async () => await Respond($"{pattern} Blocked, will {actualOnDiscovery}"),
                    async () => await Respond(response.ErrorMessage));
            }

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("remove"), Description("Remove name pattern")]
        public async Task<IResult> RemovePattern(string pattern)
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var response = await _mediator.Send(new DeleteNamePatternRequest(user, pattern));

            await response.GetAction(async () => await Respond($"{pattern} removed"),
                async () => await Respond(response.ErrorMessage));

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

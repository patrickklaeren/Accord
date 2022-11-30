using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("channel-flag"), AutoConstructor]
public partial class ChannelFlagCommandGroup: AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IMediator _mediator;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly CommandResponder _commandResponder;

    [Command("add"), Description("Add flag to the current channel")]
    public async Task<IResult> AddFlag(string type, IChannel? channel = null)
    {
        var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

        if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
        {
            await _commandResponder.Respond("Type of flag is not found");
        }
        else
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var channelId = channel?.ID.Value ?? _commandContext.ChannelID.Value;

            var response = await _mediator.Send(new AddChannelFlagRequest(user, actualChannelFlag, channelId));

            await response.GetAction(async () => await _commandResponder.Respond($"{actualChannelFlag} flag added"),
                async () => await _commandResponder.Respond(response.ErrorMessage));
        }

        return Result.FromSuccess();
    }

    [Command("remove"), Description("Add flag to the current channel")]
    public async Task<IResult> RemoveFlag(string type, IChannel? channel = null)
    {
        var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

        if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
        {
            await _commandResponder.Respond("Type of flag is not found");
        }
        else
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var channelId = channel?.ID.Value ?? _commandContext.ChannelID.Value;

            var response = await _mediator.Send(new DeleteChannelFlagRequest(user, actualChannelFlag, channelId));

            await response.GetAction(async () => await _commandResponder.Respond($"{actualChannelFlag} flag removed"),
                async () => await _commandResponder.Respond(response.ErrorMessage));
        }

        return Result.FromSuccess();
    }
}
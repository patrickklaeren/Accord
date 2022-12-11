using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Bot.Infrastructure;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("channel-flag"), AutoConstructor]
public partial class ChannelFlagCommandGroup: AccordCommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IMediator _mediator;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly FeedbackService _feedbackService;

    [Command("add"), Description("Add flag to the current channel")]
    public async Task<IResult> AddFlag(string type, IChannel? channel = null)
    {
        var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

        if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
        {
            return await _feedbackService.SendContextualAsync("Type of flag is not found");
        }

        var proxy = _commandContext.GetCommandProxy();
        var user = await _commandContext.ToPermissionUser(_guildApi);

        var channelId = channel?.ID.Value ?? proxy.ChannelId.Value;

        var response = await _mediator.Send(new AddChannelFlagRequest(user, actualChannelFlag, channelId));

        await response.GetAction(
            async () => await _feedbackService.SendContextualAsync($"{actualChannelFlag} flag added"),
            async () => await _feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("remove"), Description("Add flag to the current channel")]
    public async Task<IResult> RemoveFlag(string type, IChannel? channel = null)
    {
        var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

        if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
        {
            return await _feedbackService.SendContextualAsync("Type of flag is not found");
        }

        var user = await _commandContext.ToPermissionUser(_guildApi);

        var proxy = _commandContext.GetCommandProxy();
        var channelId = channel?.ID.Value ?? proxy.ChannelId.Value;

        var response = await _mediator.Send(new DeleteChannelFlagRequest(user, actualChannelFlag, channelId));

        await response.GetAction(
            async () => await _feedbackService.SendContextualAsync($"{actualChannelFlag} flag removed"),
            async () => await _feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }
}
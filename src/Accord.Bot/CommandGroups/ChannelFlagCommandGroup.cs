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

[Group("channel-flag")]
public class ChannelFlagCommandGroup(ICommandContext commandContext, IMediator mediator, IDiscordRestGuildAPI guildApi, FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("add"), Description("Add flag to the current channel")]
    public async Task<IResult> AddFlag(string type, IChannel? channel = null)
    {
        var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

        if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
        {
            return await feedbackService.SendContextualAsync("Type of flag is not found");
        }

        var proxy = commandContext.GetCommandProxy();
        var user = await commandContext.ToPermissionUser(guildApi);

        var channelId = channel?.ID.Value ?? proxy.ChannelId.Value;

        var response = await mediator.Send(new AddChannelFlagRequest(user, actualChannelFlag, channelId));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"{actualChannelFlag} flag added"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("remove"), Description("Add flag to the current channel")]
    public async Task<IResult> RemoveFlag(string type, IChannel? channel = null)
    {
        var isParsedEnumValue = Enum.TryParse<ChannelFlagType>(type, out var actualChannelFlag);

        if (!isParsedEnumValue || !Enum.IsDefined(actualChannelFlag))
        {
            return await feedbackService.SendContextualAsync("Type of flag is not found");
        }

        var user = await commandContext.ToPermissionUser(guildApi);

        var proxy = commandContext.GetCommandProxy();
        var channelId = channel?.ID.Value ?? proxy.ChannelId.Value;

        var response = await mediator.Send(new DeleteChannelFlagRequest(user, actualChannelFlag, channelId));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"{actualChannelFlag} flag removed"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }
}

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.UserBotMessages;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class ProfileCommandGroup(IMediator mediator, 
    ICommandContext commandContext,
    FeedbackService feedbackService, 
    ProfileEmbedFactory profileEmbedFactory) 
    : AccordCommandGroup
{
    [Command("profile", "info"), Description("Get your profile")]
    public async Task<IResult> GetProfile(IUser? userToLookup = null)
    {
        var proxy = commandContext.GetCommandProxy();
        var userId = userToLookup?.ID ?? proxy.UserId;
        var embed = await profileEmbedFactory.Create(proxy.GuildId, userId);

        if (embed is null)
        {
            return await feedbackService.SendContextualAsync("Could not retrieve user via Discord API");
        }

        var message = await feedbackService.SendContextualEmbedAsync(embed);

        if (message.IsSuccess)
        {
            await mediator.Publish(new AddUserBotMessageRequest(message.Entity.ID.Value, message.Entity.ChannelID.Value, proxy.UserId.Value));   
        }

        return message;
    }
}

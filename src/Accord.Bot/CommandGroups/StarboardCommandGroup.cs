using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Starboard;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("starboard")]
public class StarboardCommandGroup(ICommandContext commandContext, 
    IMediator mediator, 
    FeedbackService feedbackService) 
    : AccordCommandGroup
{
    [RequireDiscordPermission(DiscordPermission.Administrator), Command("post-here"), Description("Configure an option for the bot"), Ephemeral]
    public async Task<IResult> PostHere()
    {
        var commandProxy = commandContext.GetCommandProxy();
        
        var response = await mediator.Send(new AddStarboardChannelRequest(null, commandProxy.ChannelId.Value));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync("Success! This channel is now a default starboard!"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }
    
    [RequireDiscordPermission(DiscordPermission.Administrator), Command("do-not-post-here"), Description("Configure an option for the bot"), Ephemeral]
    public async Task<IResult> DoNotPostHere()
    {
        var commandProxy = commandContext.GetCommandProxy();
        
        await mediator.Send(new DeleteStarboardChannelRequest(commandProxy.ChannelId.Value));
        return await feedbackService.SendContextualAsync("Success! This channel is no longer a default starboard!");
    }
    
    [RequireDiscordPermission(DiscordPermission.Administrator), Command("post-to"), Description("Configure an option for the bot"), Ephemeral]
    public async Task<IResult> OnlyPostTo(IChannel starboardChannel)
    {
        var commandProxy = commandContext.GetCommandProxy();
        
        var response = await mediator.Send(new AddStarboardChannelRequest(commandProxy.ChannelId.Value, starboardChannel.ID.Value));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Success! This channel now only posts starred messages to {starboardChannel.Name}"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }
    
    [RequireDiscordPermission(DiscordPermission.Administrator), Command("post-to-default"), Description("Configure an option for the bot"), Ephemeral]
    public async Task<IResult> RemoveOnlyPostTo()
    {
        var commandProxy = commandContext.GetCommandProxy();
        
        await mediator.Send(new DeleteStarboardOriginChannelRequest(commandProxy.ChannelId.Value));
        return await feedbackService.SendContextualAsync("This channel now posts to the default starboard(s), if any!");
    }
}
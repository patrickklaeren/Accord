using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("mute")]
public class MuteCommandGroup(ICommandContext commandContext,
    DiscordCache discordCache,
    IDiscordRestGuildAPI guildApi,
    PermissionUserFactory permissionUserFactory,
    IMediator mediator, 
    FeedbackService feedbackService) 
    : AccordCommandGroup
{
    [Command("temp"), Description("Temporary mute a user (from text and voice)"), Ephemeral]
    public async Task<IResult> TempMute(IUser user, TimeSpan duration, [Greedy] string reason)
    {
        var proxy = commandContext.GetCommandProxy();
        var actingUser = await commandContext.ToPermissionUser(permissionUserFactory);

        var hasPermission = await mediator.Send(new UserHasPermissionRequest(actingUser,
            PermissionType.TemporaryMute));

        if (!hasPermission)
        {
            return await feedbackService.SendContextualAsync("Missing permission");
        }
        
        var selfSnowflake = await discordCache.GetSelfSnowflake();

        if (user.ID == selfSnowflake)
        {
            return await feedbackService.SendContextualAsync("Cannot mute the bot");   
        }

        if (user.ID.Value == actingUser.DiscordUserId)
        {
            return await feedbackService.SendContextualAsync("Cannot mute yourself");
        }
        
        // https://docs.discord.com/developers/resources/guild#modify-guild-member
        const int MAX_DAYS_TO_MUTE_FOR = 1;
        if (duration > TimeSpan.FromDays(MAX_DAYS_TO_MUTE_FOR))
        {
            return await feedbackService.SendContextualAsync("Temporary mutes can only last for a maximum of one day");
        }

        var now = DateTimeOffset.UtcNow;
        var muteUntil = now + duration;
        
        var modifyReason = $"On behalf of {actingUser.DiscordUserId}";

        if (!string.IsNullOrWhiteSpace(reason))
        {
            modifyReason += $" - {reason}";
        }
        
        var response = await guildApi.ModifyGuildMemberAsync(proxy.GuildId,
            user.ID,
            communicationDisabledUntil: muteUntil,
            reason: modifyReason);

        if (!response.IsSuccess)
        {
            return await feedbackService.SendContextualAsync("Failed muting user");
        }

        var targetUserMention = user.ID.ToUserMention();
        var discordTimeStamp = muteUntil.ToTimeMarkdown();

        return await feedbackService.SendContextualAsync($"{targetUserMention} muted until {discordTimeStamp}",
            options: new FeedbackMessageOptions(AllowedMentions: new AllowedMentions(Parse: new List<MentionType>())));
    }
}

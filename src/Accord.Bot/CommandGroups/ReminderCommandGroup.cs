using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Helpers;
using Accord.Services.Reminder;
using Accord.Services.Users;
using Humanizer;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("remind")]
public class ReminderCommandGroup(ICommandContext commandContext, IMediator mediator, IDiscordRestGuildAPI guildApi, DiscordAvatarHelper discordAvatarHelper, FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("me"), Description("Add a reminder for yourself")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> AddReminder(TimeSpan timeSpan, string message)
    {
        var sanitizedMessage = message.SanitiseDiscordContent();

        commandContext.TryGetUserID(out var userId);
        commandContext.TryGetChannelID(out var channelId);

        var response = await mediator.Send(new AddReminderRequest(
            userId.Value,
            channelId.Value,
            timeSpan,
            sanitizedMessage
        ));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"You will be reminded in {timeSpan.Humanize()}"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }

    [Command("list"), Description("List pending reminders"), Ephemeral]
    public async Task<IResult> ListUserReminders(int page = 1)
    {
        var proxy = commandContext.GetCommandProxy();

        var embed = await GetUserReminders(proxy.UserId, page - 1);
        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    [RequireDiscordPermission(DiscordPermission.Administrator), Command("list-user"),
     Description("List the reminders of the specified user")]
    public async Task<IResult> ListUserReminders(IGuildMember member, int page = 1)
    {
        var embed = await GetUserReminders(member.User.Value!.ID, page - 1);
        return await feedbackService.SendContextualEmbedAsync(embed);
    }

    [Command("delete"), Description("Deletes the reminder of the invoking user."), Ephemeral]
    public async Task<IResult> DeleteReminder(int reminderId)
    {
        var proxy = commandContext.GetCommandProxy();

        await mediator.Send(new DeleteReminderRequest(proxy.UserId.Value, reminderId));

        return Result.FromSuccess();
    }

    [Command("delete-user"), RequireDiscordPermission(DiscordPermission.Administrator),
     Description("Deletes a reminder of the specified user.")]
    public async Task<IResult> DeleteReminder(IGuildMember guildMember, int reminderId)
    {
        await mediator.Send(new DeleteReminderRequest(guildMember.User.Value!.ID.Value, reminderId));
        return Result.FromSuccess();
    }

    [Command("delete-all"), Description("Deletes all the reminders of the invoking user."), Ephemeral]
    public async Task<IResult> DeleteAllReminders()
    {
        var proxy = commandContext.GetCommandProxy();

        await mediator.Send(new DeleteAllRemindersRequest(proxy.UserId.Value));

        return Result.FromSuccess();
    }

    [Command("delete-user-all"), RequireDiscordPermission(DiscordPermission.Administrator),
     Description("Deletes all the reminders of the specified user.")]
    public async Task<IResult> DeleteAllReminders(IGuildMember guildMember)
    {
        await mediator.Send(new DeleteAllRemindersRequest(guildMember.User.Value!.ID.Value));
        return Result.FromSuccess();
    }

    private async Task<Embed> GetUserReminders(Snowflake id, int page = 0)
    {
        var userResponse = await mediator.Send(new GetUserRequest(id.Value));

        if (!userResponse.Success)
        {
            return new Embed(Description: userResponse.ErrorMessage);
        }

        var response = await mediator.Send(new GetRemindersRequest(id.Value));

        var proxy = commandContext.GetCommandProxy();

        var guildUserEntity = await guildApi.GetGuildMemberAsync(proxy.GuildId, id);

        if (!guildUserEntity.IsSuccess || !guildUserEntity.Entity.User.HasValue)
        {
            return new Embed(Description: "Couldn't find user in Guild");
        }

        var guildUser = guildUserEntity.Entity;
        var (userDto, _, _) = userResponse.Value!;

        var avatarUrl = discordAvatarHelper.GetAvatarUrl(guildUser.User.Value.ID.Value,
            guildUser.User.Value.Discriminator,
            guildUser.User.Value.Avatar?.Value,
            guildUser.User.Value.Avatar?.HasGif == true);

        var userHandle = !string.IsNullOrWhiteSpace(userDto.Username)
            ? userDto.Username
            : guildUser.User.Value.Username;

        var totalReminders = response.Count;

        var usedReminders = response.OrderByDescending(x => x.CreatedAt).Skip(page * 5).Take(5);
        var sb = new StringBuilder();

        var userReminders = usedReminders as UserReminder[] ?? usedReminders.ToArray();
        if (userReminders.Any())
        {
            foreach (var reminder in userReminders)
            {
                sb.AppendLine("```css");
                sb.AppendLine($"[{reminder.Id}] {reminder.Message.Truncate(10, "..."),-10} in {reminder.RemindAt.Humanize()}");
                sb.Append("```");
            }
        }
        else
        {
            sb.AppendLine("User has no reminders");
        }

        var start = 1 + page * 5;
        var end = page * 5 + userReminders.Length;

        var title = "Reminders";
        var content = sb.ToString();

        if (start <= end)
        {
            title += $" ({(start != end ? $"{start}-{end}" : $"{start}")}/{totalReminders})";
        }
        else if (page > 0)
        {
            content = "User has no more reminders";
        }

        return new Embed(Author: new EmbedAuthor(userHandle, IconUrl: avatarUrl),
            Title: title, Description: content);
    }
}

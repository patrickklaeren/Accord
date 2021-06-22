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
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    [Group("remind")]
    public class ReminderCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly DiscordAvatarHelper _discordAvatarHelper;
        private readonly CommandResponder _commandResponder;

        public ReminderCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestGuildAPI guildApi,
            CommandResponder commandResponder,
            DiscordAvatarHelper discordAvatarHelper)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _guildApi = guildApi;
            _commandResponder = commandResponder;
            _discordAvatarHelper = discordAvatarHelper;
        }

        [RequireContext(ChannelContext.Guild), Command("me"), Description("Add a reminder for the invoking user.")]
        public async Task<IResult> AddReminder(string time, string message)
        {
            var timeSpanResult = await new TimeSpanParser().TryParse(time, default);
            if (!timeSpanResult.IsSuccess)
            {
                await _commandResponder.Respond("Please provide a valid time span for your Reminder.");
                return Result.FromSuccess();
            }

            var timeSpan = timeSpanResult.Entity;
            var sanitizedMessage = message.DiscordSanitize();

            var response = await _mediator.Send(new AddReminderRequest(
                _commandContext.User.ID.Value,
                _commandContext.ChannelID.Value,
                timeSpan,
                sanitizedMessage
            ));

            await response.GetAction(
                async () => await _commandResponder.Respond($"You will be reminded about it in {time.Humanize()}"),
                async () => await _commandResponder.Respond(response.ErrorMessage)
            );

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("list"), Description("List the reminders of the invoking user.")]
        public async Task<IResult> ListUserReminders(int page = 1)
        {
            var embed = await GetUserReminders(_commandContext.User.ID, page - 1);

            await _commandResponder.Respond(embed);

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), RequireUserGuildPermission(DiscordPermission.Administrator), Command("list-user"),
         Description("List the reminders of the specified user.")]
        public async Task<IResult> ListUserReminders(IGuildMember member, int page = 1)
        {
            var embed = await GetUserReminders(member.User.Value!.ID, page - 1);

            await _commandResponder.Respond(embed);

            return Result.FromSuccess();
        }


        [RequireContext(ChannelContext.Guild), Command("delete"), Description("Deletes the reminder of the invoking user.")]
        public async Task<IResult> DeleteReminder(int reminderId)
        {
            var response = await _mediator.Send(new DeleteReminderRequest(_commandContext.User.ID.Value, reminderId));

            await response.GetAction(async () => await _commandResponder.Respond($"Your reminder has been deleted."),
                async () => await _commandResponder.Respond(response.ErrorMessage));

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("delete-user"), RequireUserGuildPermission(DiscordPermission.Administrator),
         Description("Deletes a reminder of the specified user.")]
        public async Task<IResult> DeleteReminder(IGuildMember guildMember, int reminderId)
        {
            var response = await _mediator.Send(new DeleteReminderRequest(guildMember.User.Value!.ID.Value, reminderId));

            await response.GetAction(async () => await _commandResponder.Respond($"{guildMember.User.Value.Username}'s reminder has been deleted."),
                async () => await _commandResponder.Respond(response.ErrorMessage));

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("delete-all"), Description("Deletes all the reminders of the invoking user.")]
        public async Task<IResult> DeleteAllReminders()
        {
            var response = await _mediator.Send(new DeleteAllRemindersRequest(_commandContext.User.ID.Value));

            await response.GetAction(async () => await _commandResponder.Respond($"Your reminders have been deleted."),
                async () => await _commandResponder.Respond(response.ErrorMessage));

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("delete-user-all"), RequireUserGuildPermission(DiscordPermission.Administrator),
         Description("Deletes all the reminders of the specified user.")]
        public async Task<IResult> DeleteAllReminders(IGuildMember guildMember)
        {
            var response = await _mediator.Send(new DeleteAllRemindersRequest(guildMember.User.Value!.ID.Value));

            await response.GetAction(async () => await _commandResponder.Respond($"{guildMember.User.Value.Username}'s reminders has been deleted."),
                async () => await _commandResponder.Respond(response.ErrorMessage));

            return Result.FromSuccess();
        }

        private async Task<Embed> GetUserReminders(Snowflake id, int page = 0)
        {
            var userResponse = await _mediator.Send(new GetUserRequest(id.Value));

            if (!userResponse.Success)
            {
                return new Embed(Description: userResponse.ErrorMessage);
            }

            var response = await _mediator.Send(new GetRemindersRequest(id.Value));
            if (!response.Success)
            {
                return new Embed(Description: userResponse.ErrorMessage);
            }

            var guildUserEntity = await _guildApi.GetGuildMemberAsync(_commandContext.GuildID.Value, id);

            if (!guildUserEntity.IsSuccess || guildUserEntity.Entity is null || !guildUserEntity.Entity.User.HasValue)
            {
                return new Embed(Description: "Couldn't find user in Guild");
            }

            var guildUser = guildUserEntity.Entity;
            var (userDto, _, _) = userResponse.Value!;

            var avatarUrl = _discordAvatarHelper.GetAvatarUrl(guildUser.User.Value);

            var userHandle = !string.IsNullOrWhiteSpace(userDto.UsernameWithDiscriminator)
                ? userDto.UsernameWithDiscriminator
                : DiscordHandleHelper.BuildHandle(guildUser.User.Value.Username, guildUser.User.Value.Discriminator);


            var totalReminders = response.Value!.Count;

            var usedReminders = response.Value!.OrderByDescending(x => x.CreatedAt).Skip(page * 5).Take(5);
            var sb = new StringBuilder();

            var userReminders = usedReminders as UserReminder[] ?? usedReminders.ToArray();
            if (userReminders.Any())
            {
                foreach (var reminder in userReminders)
                {
                    sb.AppendLine("```css");
                    sb.AppendLine($"[{reminder.Id}] {reminder.Message.Truncate(10, "..."),-10} in {reminder.RemindAt.Humanize(false)}");
                    sb.Append("```");
                }
            }
            else
            {
                sb.AppendLine("User has no reminders");
            }

            int start = 1 + page * 5;
            int end = page * 5 + userReminders.Length;

            string title = "Reminders";
            string content = sb.ToString();

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
}
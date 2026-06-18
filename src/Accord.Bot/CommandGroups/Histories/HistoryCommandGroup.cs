using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.UserHistories;
using Humanizer;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination.Extensions;
using Remora.Results;

namespace Accord.Bot.CommandGroups.Histories;

[Group("history")]
public class HistoryCommandGroup(ICommandContext commandContext, 
    PermissionUserFactory permissionUserFactory,
    IMediator mediator, 
    FeedbackService feedbackService,
    ThumbnailHelper thumbnailHelper) 
    : AccordCommandGroup
{
    [Command("delete"), Description("Delete a note from history")]
    public async Task<IResult> Delete(int noteId)
    {
        var user = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new DeleteUserHistoryRequest(noteId, user));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Note #{noteId:0000} removed from history"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage)
        );

        return Result.FromSuccess();
    }
    
    [Command("list"), Description("List all history for a user")]
    public async Task<IResult> List(IGuildMember member)
    {
        if (!member.User.HasValue)
        {
            return await feedbackService.SendContextualAsync("Could not find user");
        }

        var response = await mediator.Send(new GetUserHistoriesRequest(member.User.Value.ID.Value));

        if (!response.Any())
        {
            return await feedbackService.SendContextualAsync("No history notes for this user");
        }

        var pages = BuildPages(response, member.User.Value);

        var proxy = commandContext.GetCommandProxy();

        return await feedbackService.SendContextualPaginatedMessageAsync(
            proxy.UserId,
            pages
        );
    }

    private IReadOnlyList<Embed> BuildPages(IReadOnlyCollection<UserHistoryDto> histories, IUser user)
    {
        const int PAGE_SIZE = 5;
        var pageCount = (int)Math.Ceiling(histories.Count / (double)PAGE_SIZE);
        
        var pages = new List<Embed>(pageCount);
        
        var userAvatar = thumbnailHelper.GetAvatar(user);

        for (var page = 0; page < pageCount; page++)
        {
            var pageHistories = histories
                .Skip(page * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToList();

            var description = new StringBuilder();

            foreach (var history in pageHistories)
            {
                var addedByMention = DiscordFormatter.UserIdToMention(history.AddedByUserId);
                var typeLabel = history.Type.Humanize();
                var timestamp = history.AddedDateTime.ToTimeMarkdown();

                description.AppendLine($"`#{history.Id}` {typeLabel} by {addedByMention} on {timestamp}");
                description.AppendLine(history.Content.Truncate(200, "..."));
                description.AppendLine();
            }

            pages.Add(new Embed(
                Author: new EmbedAuthor(user.Username, IconUrl: userAvatar.Url),
                Description: description.ToString().TrimEnd(),
                Footer: new EmbedFooter($"Page {page + 1} of {pageCount} · {histories.Count} total notes")
            ));
        }

        return pages;
    }
}

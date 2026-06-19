using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Tags;
using Humanizer;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("tag")]
public class TagCommandGroup(ICommandContext commandContext,
    IMediator mediator,
    PermissionUserFactory permissionUserFactory,
    FeedbackService feedbackService,
    IDiscordRestInteractionAPI interactionApi) : AccordCommandGroup
{
    [Command("add"), Description("Add a tag")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> AddTag()
    {
        if (commandContext is not IInteractionCommandContext interactionContext)
        {
            return Result.FromSuccess();
        }

        var modal = new InteractionModalCallbackData(
            CustomID: "tag-add",
            Title: "Add Tag",
            Components: new IActionRowComponent[]
            {
                new ActionRowComponent([
                    new TextInputComponent("tag-name", 
                        TextInputStyle.Short, 
                        "Name",
                        MinLength: 1, 
                        MaxLength: 100, 
                        IsRequired: true,
                        Value: default, 
                        Placeholder: default)
                ]),
                new ActionRowComponent([
                    new TextInputComponent("tag-content", 
                        TextInputStyle.Paragraph, 
                        "Content",
                        MinLength: 1, 
                        MaxLength: 3500, 
                        IsRequired: true,
                        Value: default, 
                        Placeholder: default)
                ]),
            }
        );

        await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.Modal, new(modal))
        );

        return Result.FromSuccess();
    }

    [Command("edit"), Description("Edit a tag")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> EditTag(string name)
    {
        if (commandContext is not IInteractionCommandContext interactionContext)
        {
            return Result.FromSuccess();
        }

        var tag = await mediator.Send(new GetTagRequest(name));

        if (tag is null)
        {
            return Result.FromSuccess();
        }

        var modal = new InteractionModalCallbackData(
            CustomID: $"tag-edit-{tag.Id}",
            Title: "Edit Tag",
            Components: new IActionRowComponent[]
            {
                new ActionRowComponent([
                    new TextInputComponent("tag-name", 
                        TextInputStyle.Short, "Name",
                        MinLength: 1, 
                        MaxLength: 100, 
                        IsRequired: true,
                        Value: name, 
                        Placeholder: default)
                ]),
                new ActionRowComponent([
                    new TextInputComponent("tag-content", 
                        TextInputStyle.Paragraph, 
                        "Content",
                        MinLength: 1, 
                        MaxLength: 1500, 
                        IsRequired: true,
                        Value: tag?.Content ?? string.Empty, 
                        Placeholder: default)
                ]),
            }
        );

        await interactionApi.CreateInteractionResponseAsync(
            interactionContext.Interaction.ID,
            interactionContext.Interaction.Token,
            new InteractionResponse(InteractionCallbackType.Modal, new(modal))
        );

        return Result.FromSuccess();
    }

    [Command("delete"), Description("Delete a tag"), Ephemeral]
    public async Task<IResult> DeleteTag(string name)
    {
        var user = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new DeleteTagRequest(name, user));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Tag `{name}` deleted"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("alias-add"), Description("Add an alias to a tag")]
    public async Task<IResult> AddAlias(string name, string newAlias)
    {
        var user = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new AddAliasRequest(name, newAlias, user));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Alias `{newAlias}` added to `{name}`"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("alias-delete"), Description("Delete an alias from a tag")]
    public async Task<IResult> DeleteAlias(string name)
    {
        var user = await commandContext.ToPermissionUser(permissionUserFactory);

        var response = await mediator.Send(new DeleteAliasRequest(name, user));

        await response.GetAction(
            async () => await feedbackService.SendContextualAsync($"Alias `{name}` deleted"),
            async () => await feedbackService.SendContextualAsync(response.ErrorMessage));

        return Result.FromSuccess();
    }

    [Command("search"), Description("Search for tags")]
    public async Task<IResult> SearchTags(string searchTerm)
    {
        var results = await mediator.Send(new SearchTagsRequest(searchTerm));

        if (results.Count == 0)
        {
            return await feedbackService.SendContextualAsync("No tags found");
        }

        var sb = new StringBuilder();
        
        foreach (var result in results.Take(25))
        {
            sb.AppendLine($"`{result.Name}`");
        }

        if (results.Count > 25)
        {
            sb.AppendLine($"... and {results.Count - 25} more results");
        }

        return await feedbackService.SendContextualAsync(sb.ToString());
    }

    [Command("info"), Description("Get tag information")]
    public async Task<IResult> TagInfo(string name)
    {
        var tag = await mediator.Send(new GetTagRequest(name));

        if (tag is null)
        {
            return await feedbackService.SendContextualAsync("Tag not found");
        }

        var aliases = string.Join(", ", tag.Aliases.Select(a => $"`{a}`"));

        var embed = new Embed(
            Title: name,
            Description: tag.Content.Truncate(4096),
            Fields: new IEmbedField[]
            {
                new EmbedField("Aliases", aliases.Truncate(1024), false),
                new EmbedField("Uses", tag.Uses.ToString(), true),
                new EmbedField("Created by", DiscordFormatter.UserIdToMention(tag.AddedByDiscordUserId), true),
                new EmbedField("Created at", DiscordFormatter.TimeToMarkdown(tag.AddedDateTime), true),
            }
        );

        return await feedbackService.SendContextualEmbedAsync(embed);
    }
}

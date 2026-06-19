using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Tags;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class TagModalSubmitResponder(
    IMediator mediator,
    IDiscordRestInteractionAPI interactionApi,
    PermissionUserFactory permissionUserFactory
) : IResponder<IInteractionCreate>
{
    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct)
    {
        if (gatewayEvent.Type is not InteractionType.ModalSubmit)
            return Result.FromSuccess();

        if (!gatewayEvent.Data.HasValue || !gatewayEvent.Data.Value.IsT2)
            return Result.FromSuccess();

        var modalData = gatewayEvent.Data.Value.AsT2;

        if (!modalData.CustomID.StartsWith("tag-"))
            return Result.FromSuccess();

        var values = ExtractComponentValues(modalData.Components);

        if (!gatewayEvent.Member.HasValue || !gatewayEvent.Member.Value.User.HasValue)
            return Result.FromSuccess();

        var permissionUser = await permissionUserFactory.FromId(gatewayEvent.Member.Value.User.Value.ID.Value);

        ServiceResponse response;

        if (modalData.CustomID == "tag-add")
        {
            var name = values.GetValueOrDefault("tag-name", string.Empty);
            var content = values.GetValueOrDefault("tag-content", string.Empty);
            response = await mediator.Send(new AddTagRequest(name, content, permissionUser), ct);
        }
        else if (modalData.CustomID.StartsWith("tag-edit-"))
        {
            var id = int.Parse(modalData.CustomID.Replace("tag-edit-", string.Empty));
            var content = values.GetValueOrDefault("tag-content", string.Empty);
            response = await mediator.Send(new UpdateTagRequest(id, content, permissionUser), ct);
        }
        else
        {
            return Result.FromSuccess();
        }

        var messageContent = response.Success
            ? "Tag saved successfully."
            : $"Failed: {response.ErrorMessage}";

        var callback = new InteractionMessageCallbackData(
            Content: messageContent,
            Flags: MessageFlags.Ephemeral,
            AllowedMentions: new AllowedMentions(Parse: new List<MentionType>())
        );

        return await interactionApi.CreateInteractionResponseAsync(
            gatewayEvent.ID,
            gatewayEvent.Token,
            new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, new(callback))
        );
    }

    private static Dictionary<string, string> ExtractComponentValues(IReadOnlyList<IPartialMessageComponent> components)
    {
        var result = new Dictionary<string, string>();

        foreach (var component in components)
        {
            if (component is IPartialActionRowComponent actionRow && actionRow.Components.HasValue)
            {
                foreach (var innerComponent in actionRow.Components.Value)
                {
                    if (innerComponent is IPartialTextInputComponent textInput
                        && textInput.CustomID.HasValue
                        && textInput.Value.HasValue)
                    {
                        result[textInput.CustomID.Value] = textInput.Value.Value;
                    }
                }
            }
        }

        return result;
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Godbolt;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using CodeInDiscordHelper = Accord.Bot.Helpers.CodeInDiscordHelper;

namespace Accord.Bot.Responders;

public class GodboltModalSubmitResponder(
    IMediator mediator,
    IDiscordRestInteractionAPI interactionApi,
    ThumbnailHelper thumbnailHelper
) : IResponder<IInteractionCreate>
{
    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct)
    {
        if (gatewayEvent.Type is not InteractionType.ModalSubmit)
            return Result.FromSuccess();

        if (!gatewayEvent.Data.HasValue || !gatewayEvent.Data.Value.IsT2)
            return Result.FromSuccess();

        var modalData = gatewayEvent.Data.Value.AsT2;

        if (modalData.CustomID != "godbolt-compile")
            return Result.FromSuccess();

        if (!gatewayEvent.Member.HasValue || !gatewayEvent.Member.Value.User.HasValue)
            return Result.FromSuccess();

        var appId = gatewayEvent.ApplicationID;
        var token = gatewayEvent.Token;
        var user = gatewayEvent.Member.Value.User.Value;
        var avatar = thumbnailHelper.GetAvatar(user);

        var values = ExtractComponentValues(modalData.Components);
        var code = values.GetValueOrDefault("godbolt-code", string.Empty);
        var language = values.GetValueOrDefault("godbolt-language", string.Empty);
        var arguments = values.GetValueOrDefault("godbolt-arguments", string.Empty);

        var deferResult = await interactionApi.CreateInteractionResponseAsync(
            gatewayEvent.ID,
            token,
            new InteractionResponse(InteractionCallbackType.DeferredChannelMessageWithSource),
            ct: ct
        );

        if (!deferResult.IsSuccess)
            return deferResult;

        var response = await mediator.Send(new ExecuteGodboltRequest(code, language, arguments), ct);

        Embed embed;

        if (response.Success)
        {
            embed = GetSuccessEmbed(user, avatar, response.Value!);
        }
        else
        {
            embed = GetErrorEmbed(user, avatar, response.ErrorMessage);
        }

        var editResult = await interactionApi.EditOriginalInteractionResponseAsync(
            appId,
            token,
            embeds: new[] { embed }
        );

        return (Result)editResult;
    }

    private static Embed GetSuccessEmbed(IUser user, EmbedThumbnail avatar, ExecuteGodboltResultDto result)
    {
        var truncated = CodeInDiscordHelper.TruncateToEmbedField(result.Result);
        var codeBlock = CodeInDiscordHelper.FormatAsCodeBlock(truncated, "json");

        var fields = new List<EmbedField>
        {
            new("Result", codeBlock, false)
        };

        if (!string.IsNullOrWhiteSpace(result.ResultPasteUrl))
        {
            fields.Add(new EmbedField("Full output", $"[Click here]({result.ResultPasteUrl})"));
        }

        return new Embed(
            Title: "Godbolt Compilation Result",
            Colour: Color.Green,
            Author: new EmbedAuthor(user.Username, IconUrl: avatar.Url),
            Fields: fields,
            Footer: new EmbedFooter("Godbolt Compiler Explorer")
        );
    }

    private static Embed GetErrorEmbed(IUser user, EmbedThumbnail avatar, string errorMessage)
    {
        return new Embed(
            Title: "Godbolt Compilation Failed",
            Colour: Color.Red,
            Author: new EmbedAuthor(user.Username, IconUrl: avatar.Url),
            Description: errorMessage,
            Footer: new EmbedFooter("Godbolt Compiler Explorer")
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

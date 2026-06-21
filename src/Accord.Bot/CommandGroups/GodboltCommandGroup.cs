using System.ComponentModel;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class GodboltCommandGroup(ICommandContext commandContext,
    IDiscordRestInteractionAPI interactionApi) : AccordCommandGroup
{
    [Command("godbolt"), Description("Compile code using the Godbolt compiler explorer")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> Compile()
    {
        if (commandContext is not IInteractionCommandContext interactionContext)
            return Result.FromSuccess();

        var modal = new InteractionModalCallbackData(
            CustomID: "godbolt-compile",
            Title: "Godbolt Compiler Explorer",
            Components: new IActionRowComponent[]
            {
                new ActionRowComponent([
                    new TextInputComponent("godbolt-code",
                        TextInputStyle.Paragraph,
                        "Code",
                        MinLength: 1,
                        MaxLength: 4000,
                        IsRequired: true,
                        Value: default,
                        Placeholder: "Enter your code here")
                ]),
                new ActionRowComponent([
                    new TextInputComponent("godbolt-language",
                        TextInputStyle.Short,
                        "Language (csharp/fsharp/vb/il)",
                        MinLength: 1,
                        MaxLength: 10,
                        IsRequired: true,
                        Value: default,
                        Placeholder: "csharp")
                ]),
                new ActionRowComponent([
                    new TextInputComponent("godbolt-arguments",
                        TextInputStyle.Short,
                        "Arguments",
                        MinLength: 0,
                        MaxLength: 100,
                        IsRequired: false,
                        Value: default,
                        Placeholder: "Optional compiler arguments")
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
}
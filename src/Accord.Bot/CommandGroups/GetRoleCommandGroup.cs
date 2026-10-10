using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.TimedRoles;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class GetRoleCommandGroup(ICommandContext commandContext, IMediator mediator,
    IDiscordRestInteractionAPI interactionApi, DiscordConfiguration configuration) : AccordCommandGroup
{
    [Command("get-role"), Description("Request a role for 30 minutes by echoing a C# keyword")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> GetRole(IRole role)
    {
        if (commandContext is not IInteractionCommandContext context)
            return Result.FromSuccess();

        var interaction = context.Interaction;
        if (!interaction.GuildID.HasValue || interaction.GuildID.Value.Value != configuration.GuildId
            || !interaction.Member.HasValue || !interaction.Member.Value.User.HasValue)
        {
            return await interactionApi.CreateInteractionResponseAsync(interaction.ID, interaction.Token,
                new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource,
                    new(new InteractionMessageCallbackData(Content: "Use this command in the server.", Flags: MessageFlags.Ephemeral))));
        }

        var response = await mediator.Send(new StartRoleChallengeRequest(role.ID.Value));
        if (response.Failure)
        {
            return await interactionApi.CreateInteractionResponseAsync(interaction.ID, interaction.Token,
                new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource,
                    new(new InteractionMessageCallbackData(Content: response.ErrorMessage, Flags: MessageFlags.Ephemeral))));
        }

        var challenge = response.Value!;
        var modal = new InteractionModalCallbackData(
            CustomID: $"get-role-{challenge.DiscordRoleId}-{challenge.Keyword}",
            Title: "Request role for 30 minutes",
            Components: new IActionRowComponent[]
            {
                new ActionRowComponent([
                    new TextInputComponent("keyword", TextInputStyle.Short,
                        $"Type exactly: {challenge.Keyword}", MinLength: 1, MaxLength: 100,
                        IsRequired: true, Value: default, Placeholder: default)
                ])
            });
        return await interactionApi.CreateInteractionResponseAsync(interaction.ID, interaction.Token,
            new InteractionResponse(InteractionCallbackType.Modal, new(modal)));
    }
}

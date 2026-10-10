using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.TimedRoles;
using MediatR;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class GetRoleModalSubmitResponder(IMediator mediator, IDiscordRestInteractionAPI interactionApi,
    DiscordConfiguration configuration, ILogger<GetRoleModalSubmitResponder> logger) : IResponder<IInteractionCreate>
{
    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct)
    {
        if (gatewayEvent.Type != InteractionType.ModalSubmit || !gatewayEvent.Data.HasValue
            || !gatewayEvent.Data.Value.IsT2)
            return Result.FromSuccess();
        var data = gatewayEvent.Data.Value.AsT2;
        const string prefix = "get-role-";
        if (!data.CustomID.StartsWith(prefix, StringComparison.Ordinal))
            return Result.FromSuccess();

        var deferred = await interactionApi.CreateInteractionResponseAsync(gatewayEvent.ID, gatewayEvent.Token,
            new InteractionResponse(InteractionCallbackType.DeferredChannelMessageWithSource,
                new(new InteractionMessageCallbackData(Flags: MessageFlags.Ephemeral))), ct: ct);
        if (!deferred.IsSuccess)
            return deferred;

        var message = "Use this command in the server.";
        if (gatewayEvent.GuildID.HasValue && gatewayEvent.GuildID.Value.Value == configuration.GuildId
            && gatewayEvent.Member.HasValue && gatewayEvent.Member.Value.User.HasValue)
        {
            var answer = string.Empty;
            foreach (var component in data.Components)
                if (component is IPartialActionRowComponent row && row.Components.HasValue)
                    foreach (var inner in row.Components.Value)
                        if (inner is IPartialTextInputComponent input && input.CustomID.HasValue
                            && input.CustomID.Value == "keyword" && input.Value.HasValue)
                            answer = input.Value.Value;

            try
            {
                var challenge = data.CustomID[prefix.Length..].Split('-');
                if (challenge.Length != 2 || !ulong.TryParse(challenge[0], out var roleId))
                {
                    message = "Invalid role challenge. Run /get-role again.";
                }
                else
                {
                    var response = await mediator.Send(new CompleteRoleChallengeRequest(
                        gatewayEvent.Member.Value.User.Value.ID.Value, roleId, challenge[1], answer), ct);
                    message = response.Success ? "Role assigned! It will be removed after 30 minutes." : response.ErrorMessage;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed completing requested role challenge.");
                message = "Unable to complete the role request. Run /get-role again.";
            }
        }

        var result = await interactionApi.EditOriginalInteractionResponseAsync(gatewayEvent.ApplicationID,
            gatewayEvent.Token, content: message, ct: ct);
        return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result.Error);
    }
}

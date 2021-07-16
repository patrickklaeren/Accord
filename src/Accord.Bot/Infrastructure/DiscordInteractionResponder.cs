using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Results;
using Remora.Commands.Services;
using Remora.Commands.Trees;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Infrastructure
{
    public class DiscordInteractionResponder : IResponder<IInteractionCreate>
    {
        private readonly CommandService _commandService;
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly ExecutionEventCollectorService _eventCollector;
        private readonly IServiceProvider _services;
        private readonly ContextInjectionService _contextInjection;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly BotState _botState;

        public DiscordInteractionResponder(
            CommandService commandService,
            IDiscordRestInteractionAPI interactionApi,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services,
            ContextInjectionService contextInjection,
            IDiscordRestWebhookAPI webhookApi,
            IDiscordRestChannelAPI channelApi,
            BotState botState)
        {
            _commandService = commandService;
            _eventCollector = eventCollector;
            _services = services;
            _contextInjection = contextInjection;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
            _botState = botState;
            _interactionApi = interactionApi;
        }

        public async Task<Result> RespondAsync(
            IInteractionCreate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.Data.HasValue)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.ChannelID.HasValue)
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.User.HasValue
                ? gatewayEvent.User.Value
                : gatewayEvent.Member.HasValue
                    ? gatewayEvent.Member.Value.User.HasValue
                        ? gatewayEvent.Member.Value.User.Value
                        : null
                    : null;

            if (user is null)
            {
                return Result.FromSuccess();
            }

            // Signal Discord that we'll be handling this one asynchronously
            var response = new InteractionResponse(InteractionCallbackType.DeferredChannelMessageWithSource);
            var interactionResponse = await _interactionApi.CreateInteractionResponseAsync
            (
                gatewayEvent.ID,
                gatewayEvent.Token,
                response,
                ct
            );

            if (!interactionResponse.IsSuccess)
            {
                return interactionResponse;
            }

            var interactionData = gatewayEvent.Data.Value!;

            var context = new InteractionContext
            (
                gatewayEvent.GuildID,
                gatewayEvent.ChannelID.Value,
                user,
                gatewayEvent.Member,
                gatewayEvent.Token,
                gatewayEvent.ID,
                gatewayEvent.ApplicationID,
                interactionData.Resolved
            );

            // Provide the created context to any services inside this scope
            _contextInjection.Context = context;
            return await RelayResultToUserAsync
            (
                context,
                await TryExecuteCommandAsync(context, interactionData, ct),
                ct
            );
        }

        private async Task<Result> TryExecuteCommandAsync(
            ICommandContext context,
            IApplicationCommandInteractionData data,
            CancellationToken ct = default
        )
        {
            if (!_botState.IsReady)
            {
                var result = await Respond(context, "Accord is not ready yet. Try again later", ct);
                return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result.Error);
            }

            var preExecution = await _eventCollector.RunPreExecutionEvents(context, ct);
            if (!preExecution.IsSuccess)
            {
                return preExecution;
            }

            data.UnpackInteraction(out var command, out var parameters);

            // Run the actual command
            var searchOptions = new TreeSearchOptions(StringComparison.OrdinalIgnoreCase);
            var executeResult = await _commandService.TryExecuteAsync
            (
                command,
                parameters,
                _services,
                searchOptions: searchOptions,
                ct: ct
            );

            if (!executeResult.IsSuccess)
            {
                return Result.FromError(executeResult);
            }

            // Run any user-provided post execution events
            var postExecution = await _eventCollector.RunPostExecutionEvents
            (
                context,
                executeResult.Entity,
                ct
            );

            if (!postExecution.IsSuccess)
            {
                return postExecution;
            }

            return executeResult.Entity.IsSuccess ? Result.FromSuccess() : Result.FromError(executeResult.Error!);
        }

        private async Task<Result> RelayResultToUserAsync<TResult>(
            InteractionContext context,
            TResult commandResult,
            CancellationToken ct = default
        )
            where TResult : IResult
        {
            if (commandResult.IsSuccess)
                return Result.FromSuccess();

            IResult result = commandResult;
            while (result.Inner is not null)
            {
                result = result.Inner;
            }

            var error = result.Unwrap();
            switch (error)
            {
                case ParameterParsingError:
                case AmbiguousCommandInvocationError:
                case ConditionNotSatisfiedError:
                case { } when error.GetType().IsGenericType &&
                              error.GetType().GetGenericTypeDefinition() == typeof(ParsingError<>):
                    {
                        var sendError = await Respond(context, error.Message, ct);
                        return sendError.IsSuccess
                            ? Result.FromSuccess()
                            : Result.FromError(sendError);
                    }
                default:
                    {
                        return Result.FromError(commandResult.Unwrap());
                    }
            }
        }

        public async Task<Result<IMessage>> Respond(ICommandContext commandContext, string message, CancellationToken ct = default)
        {
            if (commandContext is InteractionContext interactionContext)
            {
                return await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, content: message, ct: ct);
            }

            return await _channelApi.CreateMessageAsync(commandContext.ChannelID, content: message, ct: ct);
        }

        public async Task<Result<IMessage>> Respond(ICommandContext commandContext, Embed embed, CancellationToken ct = default)
        {
            if (commandContext is InteractionContext interactionContext)
            {
                return await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, embeds: new[] {embed}, ct: ct);
            }

            return await _channelApi.CreateMessageAsync(commandContext.ChannelID, embeds: new [] { embed }, ct: ct);
        }
    }
}
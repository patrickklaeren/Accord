using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Remora.Commands.Results;
using Remora.Commands.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Infrastructure
{
    public class DiscordCommandResponder : IResponder<IMessageCreate>, IResponder<IMessageUpdate>
    {
        private readonly BotState _botState;
        private readonly CommandService _commandService;
        private readonly DiscordCommandResponderOptions _options;
        private readonly ExecutionEventCollectorService _eventCollector;
        private readonly IServiceProvider _services;
        private readonly ContextInjectionService _contextInjection;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public DiscordCommandResponder(CommandService commandService,
            ExecutionEventCollectorService eventCollector,
            IServiceProvider services,
            ContextInjectionService contextInjection,
            BotState botState,
            IOptions<DiscordCommandResponderOptions> options,
            IDiscordRestWebhookAPI webhookApi,
            IDiscordRestChannelAPI channelApi)
        {
            _commandService = commandService;
            _eventCollector = eventCollector;
            _services = services;
            _contextInjection = contextInjection;
            _botState = botState;
            _options = options.Value;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
        }

        public async Task<Result> RespondAsync(
            IMessageCreate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return Result.FromSuccess();
            }

            if (_options.Prefix is not null)
            {
                if (!gatewayEvent.Content.StartsWith(_options.Prefix))
                {
                    return Result.FromSuccess();
                }
            }

            var author = gatewayEvent.Author;
            if (author.IsBot.HasValue && author.IsBot.Value)
            {
                return Result.FromSuccess();
            }

            if (author.IsSystem.HasValue && author.IsSystem.Value)
            {
                return Result.FromSuccess();
            }

            var context = new MessageContext
            (
                gatewayEvent.ChannelID,
                author,
                gatewayEvent.ID,
                new PartialMessage
                (
                    gatewayEvent.ID,
                    gatewayEvent.ChannelID,
                    gatewayEvent.GuildID,
                    new Optional<IUser>(gatewayEvent.Author),
                    gatewayEvent.Member,
                    gatewayEvent.Content,
                    gatewayEvent.Timestamp,
                    gatewayEvent.EditedTimestamp,
                    gatewayEvent.IsTTS,
                    gatewayEvent.MentionsEveryone,
                    new Optional<IReadOnlyList<IUserMention>>(gatewayEvent.Mentions),
                    new Optional<IReadOnlyList<Snowflake>>(gatewayEvent.MentionedRoles),
                    gatewayEvent.MentionedChannels,
                    new Optional<IReadOnlyList<IAttachment>>(gatewayEvent.Attachments),
                    new Optional<IReadOnlyList<IEmbed>>(gatewayEvent.Embeds),
                    gatewayEvent.Reactions,
                    gatewayEvent.Nonce,
                    gatewayEvent.IsPinned,
                    gatewayEvent.WebhookID,
                    gatewayEvent.Type,
                    gatewayEvent.Activity,
                    gatewayEvent.Application,
                    gatewayEvent.ApplicationID,
                    gatewayEvent.MessageReference,
                    gatewayEvent.Flags
                )
            );

            _contextInjection.Context = context;

            return await RelayResultToUserAsync(
                context,
                await ExecuteCommandAsync(gatewayEvent.Content, context, ct),
                ct);
        }

        public async Task<Result> RespondAsync(
            IMessageUpdate? gatewayEvent,
            CancellationToken ct = default
        )
        {
            if (gatewayEvent is null)
            {
                return Result.FromSuccess();
            }

            if (!gatewayEvent.Content.HasValue)
            {
                return Result.FromSuccess();
            }

            if (_options.Prefix is not null)
            {
                if (!gatewayEvent.Content.Value.StartsWith(_options.Prefix))
                {
                    return Result.FromSuccess();
                }
            }

            if (!gatewayEvent.Author.HasValue)
            {
                return Result.FromSuccess();
            }

            var author = gatewayEvent.Author.Value!;
            if (author.IsBot.HasValue && author.IsBot.Value)
            {
                return Result.FromSuccess();
            }

            if (author.IsSystem.HasValue && author.IsSystem.Value)
            {
                return Result.FromSuccess();
            }

            var context = new MessageContext
            (
                gatewayEvent.ChannelID.Value,
                author,
                gatewayEvent.ID.Value,
                gatewayEvent
            );
            _contextInjection.Context = context;
            return await RelayResultToUserAsync(
                context,
                await ExecuteCommandAsync(gatewayEvent.Content.Value!, context, ct),
                ct);
        }

        private async Task<Result> ExecuteCommandAsync(
            string content,
            ICommandContext commandContext,
            CancellationToken ct = default
        )
        {
            if (!_botState.IsReady)
            {
                var result = await Respond(commandContext, "Accord is not ready yet. Try again later", ct);
                return result.IsSuccess ? Result.FromSuccess() : Result.FromError(result.Error);
            }

            // Provide the created context to any services inside this scope
            _contextInjection.Context = commandContext;

            // Strip off the prefix
            if (_options.Prefix is not null)
            {
                content = content
                [
                    (content.IndexOf(_options.Prefix, StringComparison.Ordinal) + _options.Prefix.Length)..
                ];
            }

            // Run any user-provided pre execution events
            var preExecution = await _eventCollector.RunPreExecutionEvents(commandContext, ct);
            if (!preExecution.IsSuccess)
            {
                return preExecution;
            }

            // Run the actual command
            var executeResult = await _commandService.TryExecuteAsync
            (
                content,
                _services,
                ct: ct
            );

            if (!executeResult.IsSuccess)
            {
                return Result.FromError(executeResult);
            }

            // Run any user-provided post execution events
            var postExecution = await _eventCollector.RunPostExecutionEvents
            (
                commandContext,
                executeResult.Entity,
                ct
            );

            return postExecution.IsSuccess
                ? Result.FromSuccess()
                : postExecution;
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

            return await _channelApi.CreateMessageAsync(commandContext.ChannelID, embeds: new[] { embed }, ct: ct);
        }

        private async Task<Result> RelayResultToUserAsync<TResult>(
            ICommandContext context,
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

            var error = result.Error!;
            switch (error)
            {
                case ParameterParsingError:
                case AmbiguousCommandInvocationError:
                case ConditionNotSatisfiedError:
                case { } when error.GetType().IsGenericType &&
                              error.GetType().GetGenericTypeDefinition() == typeof(ParsingError<>):
                    // Alert the user, and don't complete the transaction
                    var sendError = await Respond
                    (
                        context,
                        error.Message,
                        ct
                    );

                    return sendError.IsSuccess
                        ? Result.FromSuccess()
                        : Result.FromError(sendError);
                default:
                    return Result.FromError(commandResult.Error!);
            }
        }
    }
}
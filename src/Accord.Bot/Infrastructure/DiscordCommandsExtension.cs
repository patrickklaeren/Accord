using System;
using Accord.Bot.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Parsers;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Extensions;
using RequireContextCondition = Accord.Bot.Infrastructure.Undocumented.RequireContextCondition;

namespace Accord.Bot.Infrastructure
{
    public static class AccordCommands
    {
        public static IServiceCollection AddDiscordCommands(this IServiceCollection serviceCollection, bool enableSlash)
        {
            serviceCollection
                .TryAddScoped<ContextInjectionService>();

            serviceCollection
                .TryAddTransient<ICommandContext>
                (
                    s =>
                    {
                        var injectionService = s.GetRequiredService<ContextInjectionService>();
                        return injectionService.Context ?? throw new InvalidOperationException
                        (
                            "No context has been set for this scope."
                        );
                    }
                );

            serviceCollection
                .TryAddTransient
                (
                    s =>
                    {
                        var injectionService = s.GetRequiredService<ContextInjectionService>();
                        return injectionService.Context as MessageContext ?? throw new InvalidOperationException
                        (
                            "No message context has been set for this scope."
                        );
                    }
                );

            serviceCollection
                .TryAddTransient
                (
                    s =>
                    {
                        var injectionService = s.GetRequiredService<ContextInjectionService>();
                        return injectionService.Context as InteractionContext ?? throw new InvalidOperationException
                        (
                            "No interaction context has been set for this scope."
                        );
                    }
                );

            serviceCollection.AddCommands();
            serviceCollection.AddCommandResponder();

            serviceCollection.AddCondition<RequireContextCondition>();
            serviceCollection.AddCondition<RequireOwnerCondition>();
            serviceCollection.AddCondition<RequireUserGuildPermissionCondition>();

            serviceCollection
                .AddParser<IChannel, DiscordChannelParser>()
                .AddParser<IGuildMember, GuildMemberParser>()
                .AddParser<IRole, RoleParser>()
                .AddParser<IUser, UserParser>()
                .AddParser<Snowflake, SnowflakeParser>();

            serviceCollection.TryAddScoped<ExecutionEventCollectorService>();

            if (!enableSlash)
            {
                return serviceCollection;
            }

            serviceCollection.TryAddSingleton<SlashService>();
            serviceCollection.AddInteractionResponder();

            return serviceCollection;
        }

        public static IServiceCollection AddCommandResponder(
            this IServiceCollection serviceCollection,
            Action<DiscordCommandResponderOptions>? optionsConfigurator = null
        )
        {
            optionsConfigurator ??= options => options.Prefix = "!";

            serviceCollection.AddResponder<DiscordCommandResponder>();
            serviceCollection.Configure(optionsConfigurator);

            return serviceCollection;
        }

        public static IServiceCollection AddInteractionResponder(
            this IServiceCollection serviceCollection
        )
        {
            serviceCollection.AddResponder<DiscordInteractionResponder>();
            return serviceCollection;
        }
    }
}
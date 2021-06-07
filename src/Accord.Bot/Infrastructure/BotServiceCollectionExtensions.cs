using Accord.Bot.CommandGroups;
using Accord.Bot.Responders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;

namespace Accord.Bot.Infrastructure
{
    public static class BotServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
        {
            var discordConfigurationSection = configuration.GetSection("DiscordConfiguration");

            var token = discordConfigurationSection["BotToken"];

            services
                .Configure<DiscordConfiguration>(discordConfigurationSection);

            services
                .AddLogging()
                .AddTransient<BotClient>()
                .AddDiscordGateway(_ => token)
                .AddDiscordCommands(true)
                .AddCommandGroup<XpCommandGroup>()
                .AddResponder<ReadyResponder>()
                .AddResponder<XpResponder>();

            return services;
        }
    }
}
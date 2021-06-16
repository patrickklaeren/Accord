using Accord.Bot.CommandGroups;
using Accord.Bot.Responders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
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
                .Configure<DiscordGatewayClientOptions>(o =>
                {
                    o.Intents |= GatewayIntents.GuildPresences;
                    o.Intents |= GatewayIntents.GuildVoiceStates;
                    o.Intents |= GatewayIntents.GuildMembers;
                    o.Intents |= GatewayIntents.GuildMessages;
                })
                .AddDiscordCommands(true)
                .AddCommandGroup<XpCommandGroup>()
                .AddCommandGroup<ChannelFlagCommandGroup>()
                .AddCommandGroup<PermissionCommandGroup>()
                .AddCommandGroup<RunOptionCommandGroup>()
                .AddResponder<ReadyResponder>()
                .AddResponder<JoinLeaveResponder>()
                .AddResponder<MessageCreateDeleteResponder>()
                .AddResponder<VoiceStateResponder>()
                .AddResponder<XpResponder>();

            return services;
        }
    }
}
using System;
using Accord.Bot.CommandGroups;
using Accord.Bot.Helpers;
using Accord.Bot.Responders;
using Accord.Services.Reminder;
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
                .AddScoped<DiscordAvatarHelper>()
                .AddScoped<CommandResponder>()
                .AddDiscordGateway(_ => token)
                .Configure<DiscordGatewayClientOptions>(o =>
                {
                    o.Intents |= GatewayIntents.GuildPresences;
                    o.Intents |= GatewayIntents.GuildVoiceStates;
                    o.Intents |= GatewayIntents.GuildMembers;
                    o.Intents |= GatewayIntents.GuildMessages;
                })
                .AddHostedService<RemindersHostedService>()
                .AddDiscordCommands(true)
                .AddParser<TimeSpan, TimeSpanParser>()
                .AddCommandGroup<XpCommandGroup>()
                .AddCommandGroup<ChannelFlagCommandGroup>()
                .AddCommandGroup<PermissionCommandGroup>()
                .AddCommandGroup<RunOptionCommandGroup>()
                .AddCommandGroup<ReminderCommandGroup>()
                .AddCommandGroup<NamePatternCommandGroup>()
                .AddCommandGroup<ProfileCommandGroup>()
                .AddResponder<ReadyResponder>()
                .AddResponder<MemberJoinLeaveResponder>()
                .AddResponder<MemberUpdateResponder>()
                .AddResponder<MessageCreateDeleteResponder>()
                .AddResponder<VoiceStateResponder>()
                .AddResponder<XpResponder>();

            return services;
        }
    }
}
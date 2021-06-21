using System.Linq;
using System;
using Accord.Bot.CommandGroups;
using Accord.Bot.CommandGroups.UserReports;
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
                .AddScoped<DiscordCache>()
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
                .AddCommandGroup<UserReportCommandGroup>()
                .AddCommandGroup<ReportCommandGroup>();

            var responderTypes = typeof(BotClient).Assembly
                .GetExportedTypes()
                .Where(t => t.IsResponder());

            foreach (var responderType in responderTypes)
            {
                services.AddResponder(responderType);
            }

            return services;
        }
    }
}
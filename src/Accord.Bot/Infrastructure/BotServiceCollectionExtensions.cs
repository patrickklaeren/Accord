using System.Linq;
using Accord.Bot.CommandGroups;
using Accord.Bot.CommandGroups.UserReports;
using Accord.Bot.Helpers;
using Accord.Bot.Helpers.Permissions;
using Accord.Bot.Parsers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;

namespace Accord.Bot.Infrastructure;

public static class BotServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        var discordConfigurationSection = configuration.GetSection("DiscordConfiguration");

        var token = discordConfigurationSection["BotToken"];

        services
            .Configure<DiscordConfiguration>(discordConfigurationSection);

        services
            .Configure<DiscordCommandResponderOptions>(o => o.Prefix = "!");

        services
            .AddLogging()
            .AddTransient<BotClient>()
            .AddSingleton<BotState>()
            .AddSingleton<DiscordCache>()
            .AddScoped<DiscordAvatarHelper>()
            .AddScoped<DiscordPermissionHelper>()
            .AddScoped<DiscordScopedCache>()
            .AddScoped<DiscordChannelParser>()
            .AddScoped<CommandResponder>()
            .AddDiscordGateway(_ => token)
            .Configure<DiscordGatewayClientOptions>(o =>
            {
                o.Intents |= GatewayIntents.MessageContents;
                o.Intents |= GatewayIntents.GuildPresences;
                o.Intents |= GatewayIntents.GuildVoiceStates;
                o.Intents |= GatewayIntents.GuildMembers;
                o.Intents |= GatewayIntents.GuildMessages;
            })
            .AddHostedService<RemindersHostedService>()
            .AddDiscordCommands(true)
            .AddPostExecutionEvent<AfterCommandPostExecutionEvent>()
            .AddParser<TimeSpanParser>();

        services.AddCommandTree()
            .WithCommandGroup<XpCommandGroup>()
            .WithCommandGroup<GitHubChallengesCommandGroup>()
            .WithCommandGroup<ChannelFlagCommandGroup>()
            .WithCommandGroup<UserChannelHidingCommandGroup>()
            .WithCommandGroup<PermissionCommandGroup>()
            .WithCommandGroup<RunOptionCommandGroup>()
            .WithCommandGroup<ReminderCommandGroup>()
            .WithCommandGroup<NamePatternCommandGroup>()
            .WithCommandGroup<ProfileCommandGroup>()
            .WithCommandGroup<UserReportCommandGroup>()
            .WithCommandGroup<ReportCommandGroup>()
            .WithCommandGroup<LgtmCommandGroup>()
            .WithCommandGroup<QuestionThreadCommandGroup>();

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
﻿using System.Linq;
using Accord.Bot.CommandGroups;
using Accord.Bot.CommandGroups.UserReports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Interactivity.Extensions;

namespace Accord.Bot.Infrastructure;

public static class BotServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        var discordConfigurationSection = configuration.GetSection("Discord");

        var token = discordConfigurationSection["BotToken"]!;

        services
            .Configure<DiscordCommandResponderOptions>(o => o.Prefix = "!");

        services
            .AddLogging()
            .AutoRegister()
            .AddDiscordGateway(_ => token)
            .Configure<DiscordGatewayClientOptions>(o =>
            {
                o.Intents |= GatewayIntents.MessageContents;
                o.Intents |= GatewayIntents.GuildPresences;
                o.Intents |= GatewayIntents.GuildVoiceStates;
                o.Intents |= GatewayIntents.GuildMembers;
                o.Intents |= GatewayIntents.GuildMessages;
            })
            .AddDiscordCommands(true)
            .AddPostExecutionEvent<AfterCommandPostExecutionEvent>()
            .AddParser<TimeSpanParser>();

        services
            .AddCommandTree()
            .WithCommandGroup<ParticipationCommandGroup>()
            .WithCommandGroup<GitHubChallengesCommandGroup>()
            .WithCommandGroup<ChannelFlagCommandGroup>()
            .WithCommandGroup<UserChannelHidingCommandGroup>()
            .WithCommandGroup<PermissionCommandGroup>()
            .WithCommandGroup<RunOptionCommandGroup>()
            .WithCommandGroup<ReminderCommandGroup>()
            .WithCommandGroup<ProfileCommandGroup>()
            .WithCommandGroup<UserReportCommandGroup>()
            .WithCommandGroup<ReportCommandGroup>()
            .WithCommandGroup<LgtmCommandGroup>()
            .WithCommandGroup<HelpForumCommandGroup>();

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

using System;
using System.Linq;
using Accord.Bot.CommandGroups;
using Accord.Bot.CommandGroups.Histories;
using Accord.Bot.Infrastructure;
using Accord.Bot.Responders.Eval;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Pagination.Extensions;

namespace Accord.Bot;

public static class BotServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordBot(this IServiceCollection services, IConfiguration configuration)
    {
        var discordConfiguration = configuration.GetSection("Discord");

        services
            .Configure<DiscordCommandResponderOptions>(o => o.Prefix = "!");

        services
            .AddLogging()
            .AutoRegister()
            .AddDiscordGateway(_ => discordConfiguration["BotToken"]!)
            .Configure<DiscordGatewayClientOptions>(o =>
            {
                o.Intents |= GatewayIntents.MessageContents;
                o.Intents |= GatewayIntents.Guilds;
                o.Intents |= GatewayIntents.GuildBans;
                o.Intents |= GatewayIntents.GuildMembers;
                o.Intents |= GatewayIntents.GuildMessages;
                o.Intents |= GatewayIntents.GuildMessageReactions;
                o.Intents |= GatewayIntents.GuildVoiceStates;
            })
            .AddDiscordCommands(true)
            .AddPostExecutionEvent<AfterCommandPostExecutionEvent>()
            .AddParser<TimeSpanParser>();

        services.AddHttpClient("repl", x =>
        {
            x.BaseAddress = new Uri(configuration["ReplBaseUrl"]!);
        })
        .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(5)));

        services
            .AddPagination()
            .AddCommandTree()
            .WithCommandGroup<ParticipationCommandGroup>()
            .WithCommandGroup<GitHubChallengesCommandGroup>()
            .WithCommandGroup<ChannelFlagCommandGroup>()
            .WithCommandGroup<PermissionCommandGroup>()
            .WithCommandGroup<RunOptionCommandGroup>()
            .WithCommandGroup<ReminderCommandGroup>()
            .WithCommandGroup<ProfileCommandGroup>()
            .WithCommandGroup<HelpForumCommandGroup>()
            .WithCommandGroup<HistoryCommandGroup>()
            .WithCommandGroup<NoteCommandGroup>()
            .WithCommandGroup<TagCommandGroup>();

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

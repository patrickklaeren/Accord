using System;
using Accord.Bot.CommandGroups;
using Accord.Bot.CommandGroups.Histories;
using Accord.Bot.Infrastructure;
using Accord.Bot.Responders;
using Accord.Services.CodeEvaluation;
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
                o.Intents |= GatewayIntents.MessageContent;
                o.Intents |= GatewayIntents.Guilds;
                o.Intents |= GatewayIntents.GuildModeration;
                o.Intents |= GatewayIntents.GuildMembers;
                o.Intents |= GatewayIntents.GuildMessages;
                o.Intents |= GatewayIntents.GuildMessageReactions;
                o.Intents |= GatewayIntents.GuildVoiceStates;
            })
            .AddDiscordCommands(true)
            .AddPostExecutionEvent<AfterCommandPostExecutionEvent>()
            .AddParser<TimeSpanParser>();

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
            .WithCommandGroup<TagCommandGroup>()
            .WithCommandGroup<GodboltCommandGroup>();

        services
            .AddResponder<ChannelUpdateResponder>()
            .AddResponder<GuildUpdateResponder>()
            .AddResponder<MemberJoinLeaveResponder>()
            .AddResponder<MemberUpdateResponder>()
            .AddResponder<MessageCreateDeleteResponder>()
            .AddResponder<ModerationActionResponder>()
            .AddResponder<ReactionResponder>()
            .AddResponder<ReadyResponder>()
            .AddResponder<RoleUpdateResponder>()
            .AddResponder<TagModalSubmitResponder>()
            .AddResponder<TagResponder>()
            .AddResponder<VoiceStateResponder>()
            .AddResponder<EvalResponder>()
            .AddResponder<GodboltModalSubmitResponder>()
            .AddResponder<UnknownEventResponder>();

        return services;
    }
}

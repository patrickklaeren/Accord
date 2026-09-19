using Accord.Bot.Autocomplete;
using Accord.Bot.CommandGroups;
using Accord.Bot.Infrastructure;
using Accord.Bot.Responders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            .AddParser<TimeSpanParser>()
            .AddAutocompleteProvider<DiagnosticAutocompleteProvider>();

        services
            .AddPagination()
            .AddCommandTree()
            .WithCommandGroup<ParticipationCommandGroup>()
            .WithCommandGroup<ChannelFlagCommandGroup>()
            .WithCommandGroup<PermissionCommandGroup>()
            .WithCommandGroup<CampaignCommandGroup>()
            .WithCommandGroup<RunOptionCommandGroup>()
            .WithCommandGroup<ReminderCommandGroup>()
            .WithCommandGroup<ProfileCommandGroup>()
            .WithCommandGroup<HistoryCommandGroup>()
            .WithCommandGroup<NoteCommandGroup>()
            .WithCommandGroup<TagCommandGroup>()
            .WithCommandGroup<GodboltCommandGroup>()
            .WithCommandGroup<LinkShortnerCommandGroup>()
            .WithCommandGroup<StarboardCommandGroup>()
            .WithCommandGroup<HelpCommandGroup>()
            .WithCommandGroup<LookupCommandGroup>()
            .WithCommandGroup<MuteCommandGroup>()
            .WithCommandGroup<UnmuteCommandGroup>()
            .WithCommandGroup<ChangelogCommandGroup>()
            .WithCommandGroup<HelpForumCommandGroup>()
            .WithCommandGroup<RssCommandGroup>()
            .WithCommandGroup<VoiceChannelLeasingCommandGroup>();

        services
            .AddResponder<ChannelUpdateResponder>()
            .AddResponder<GuildUpdateResponder>()
            .AddResponder<MemberJoinLeaveResponder>()
            .AddResponder<MemberUpdateResponder>()
            .AddResponder<MessageCreateDeleteResponder>()
            .AddResponder<ModerationActionResponder>()
            .AddResponder<PromotionCampaignVoteResponder>()
            .AddResponder<ReactionResponder>()
            .AddResponder<ReadyResponder>()
            .AddResponder<RoleUpdateResponder>()
            .AddResponder<TagModalSubmitResponder>()
            .AddResponder<TagResponder>()
            .AddResponder<VoiceStateResponder>()
            .AddResponder<VoiceChannelLeaseSeedResponder>()
            .AddResponder<EvalResponder>()
            .AddResponder<GodboltModalSubmitResponder>()
            .AddResponder<LinkedMessageResponder>()
            .AddResponder<UnknownEventResponder>();

        return services;
    }
}

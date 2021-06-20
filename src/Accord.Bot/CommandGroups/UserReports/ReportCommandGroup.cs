using System.ComponentModel;
using System.Numerics;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.UserReports;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups.UserReports
{
    public class ReportCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly CommandResponder _commandResponder;
        private readonly DiscordCache _discordCache;

        public ReportCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestGuildAPI guildApi,
            CommandResponder commandResponder,
            DiscordCache discordCache,
            IDiscordRestChannelAPI channelApi)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _guildApi = guildApi;
            _commandResponder = commandResponder;
            _discordCache = discordCache;
            _channelApi = channelApi;
        }

        [RequireContext(ChannelContext.Guild), Command("report"), Description("Start a user report")]
        public async Task<IResult> Setup()
        {
            var response = await _mediator.Send(new GetIsUserReportsEnabledRequest());

            if (!response)
            {
                await _commandResponder.Respond("User reports are disabled!");
                return Result.FromSuccess();
            }

            var existingReport = await _mediator.Send(new GetExistingOutboxReportForUserRequest(_commandContext.User.ID.Value));

            if (existingReport.HasExistingReport)
            {
                // TODO Handle existing report
            }
            else
            {
                var everyoneRole = await _discordCache.GetEveryoneRole(_commandContext.GuildID.Value);

                var outboxCategoryId = await _mediator.Send(new GetUserReportsOutboxCategoryIdRequest());
                var inboxCategoryId = await _mediator.Send(new GetUserReportsInboxCategoryIdRequest());

                var agentRoleId = await _mediator.Send(new GetUserReportsAgentRoleIdRequest());

                var reporterPermissionOverwrite = new PermissionOverwrite(
                    _commandContext.User.ID,
                    PermissionOverwriteType.Member,
                    new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages),
                    new DiscordPermissionSet(BigInteger.Zero));

                var agentsPermissionOverwrite = new PermissionOverwrite(
                    new Snowflake(agentRoleId!.Value),
                    PermissionOverwriteType.Role,
                    new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages),
                    new DiscordPermissionSet(BigInteger.Zero));

                var everyonePermissionOverwrite = new PermissionOverwrite(
                    everyoneRole.ID,
                    PermissionOverwriteType.Role,
                    new DiscordPermissionSet(BigInteger.Zero),
                    new DiscordPermissionSet(DiscordPermission.ViewChannel));

                var outboxChannel = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                    $"{_commandContext.User.Username}-{_commandContext.User.Discriminator.ToPaddedDiscriminator()}",
                    ChannelType.GuildText,
                    parentID: new Snowflake(outboxCategoryId!.Value),
                    permissionOverwrites: new[] { reporterPermissionOverwrite, everyonePermissionOverwrite });

                if (!outboxChannel.IsSuccess)
                {
                    await _commandResponder.Respond("Failed creating report!");
                    return Result.FromError(outboxChannel.Error);
                }

                var inboxChannel = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                    $"{_commandContext.User.Username}-{_commandContext.User.Discriminator.ToPaddedDiscriminator()}-inbox",
                    ChannelType.GuildText,
                    parentID: new Snowflake(inboxCategoryId!.Value),
                    permissionOverwrites: new[] { agentsPermissionOverwrite, everyonePermissionOverwrite });

                if (!inboxChannel.IsSuccess)
                {
                    // TODO In the event the inbox doesn't get created, delete the outbox pair channel
                    await _commandResponder.Respond("Failed creating report!");
                    return Result.FromError(inboxChannel.Error);
                }

                await _mediator.Send(new AddReportRequest(_commandContext.User.ID.Value, 
                    outboxChannel.Entity.ID.Value, 
                    inboxChannel.Entity.ID.Value));

                await _commandResponder.Respond($"Report created, go to {outboxChannel.Entity.ID.ToChannelMention()}");

                await _channelApi.CreateMessageAsync(outboxChannel.Entity.ID, $"Start your report here {_commandContext.User.ID.ToUserMention()}");
            }

            return Result.FromSuccess();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Helpers;
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
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly CommandResponder _commandResponder;
        private readonly DiscordAvatarHelper _avatarHelper;
        private readonly DiscordScopedCache _discordCache;
        private readonly IEventQueue _eventQueue;

        public ReportCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestGuildAPI guildApi,
            CommandResponder commandResponder,
            IDiscordRestChannelAPI channelApi,
            DiscordAvatarHelper avatarHelper,
            DiscordScopedCache discordCache,
            IEventQueue eventQueue,
            IDiscordRestWebhookAPI webhookApi)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _guildApi = guildApi;
            _commandResponder = commandResponder;
            _channelApi = channelApi;
            _avatarHelper = avatarHelper;
            _discordCache = discordCache;
            _eventQueue = eventQueue;
            _webhookApi = webhookApi;
        }

        [RequireContext(ChannelContext.Guild), Command("report"), Description("Start a user report")]
        public async Task<IResult> Report()
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
                await _commandResponder.Respond(
                    $"You have already an open report. Visit {DiscordFormatter.ChannelIdToMention(existingReport.OutboxDiscordChannelId.Value)} to continue with your report");
                await _channelApi.CreateMessageAsync(new Snowflake(existingReport.OutboxDiscordChannelId.Value),
                    $"{DiscordFormatter.UserIdToMention(_commandContext.User.ID.Value)} Here!");
                return Result.FromSuccess();
            }

            var outboxCategoryId = await _mediator.Send(new GetUserReportsOutboxCategoryIdRequest());
            var inboxCategoryId = await _mediator.Send(new GetUserReportsInboxCategoryIdRequest());
            var agentRoleId = await _mediator.Send(new GetUserReportsAgentRoleIdRequest());
            var everyoneRole = _discordCache.GetEveryoneRole();

            if (agentRoleId is null)
            {
                // TODO Figure out how to panic here
                throw new InvalidOperationException("Cannot create report when there is no agent role");
            }

            var everyonePermissionOverwrite = new PermissionOverwrite(
                everyoneRole.ID,
                PermissionOverwriteType.Role,
                new DiscordPermissionSet(BigInteger.Zero),
                new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages));

            var selfBotPermissionOverwrite = new PermissionOverwrite(
                _discordCache.GetSelfSnowflake(),
                PermissionOverwriteType.Member,
                new DiscordPermissionSet(DiscordPermission.ViewChannel,
                    DiscordPermission.SendMessages,
                    DiscordPermission.ManageChannels,
                    DiscordPermission.ManageWebhooks,
                    DiscordPermission.ManageMessages),
                new DiscordPermissionSet(BigInteger.Zero));

            var reporterPermissionOverwrite = new PermissionOverwrite(
                _commandContext.User.ID,
                PermissionOverwriteType.Member,
                new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages),
                new DiscordPermissionSet(BigInteger.Zero));

            var outboxChannel = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                $"{_commandContext.User.Username}-{_commandContext.User.Discriminator.ToPaddedDiscriminator()}",
                ChannelType.GuildText,
                parentID: new Snowflake(outboxCategoryId!.Value),
                permissionOverwrites: new[] {selfBotPermissionOverwrite, everyonePermissionOverwrite, reporterPermissionOverwrite});

            if (!outboxChannel.IsSuccess)
            {
                await _commandResponder.Respond("Failed creating report!");
                return Result.FromError(outboxChannel.Error);
            }

            var outboxChannelMessageProxyWebhook = await _webhookApi.CreateWebhookAsync(
                outboxChannel.Entity!.ID,
                $"{outboxChannel.Entity.Name.Value}Proxy",
                null
            );

            if (!outboxChannelMessageProxyWebhook.IsSuccess)
                return await RevertReportCreation(outboxChannel);

            var inboxChannel = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                $"{_commandContext.User.Username}-{_commandContext.User.Discriminator.ToPaddedDiscriminator()}-inbox",
                ChannelType.GuildText,
                parentID: new Snowflake(inboxCategoryId!.Value));

            if (!inboxChannel.IsSuccess)
                return await RevertReportCreation(outboxChannel, outboxChannelMessageProxyWebhook);

            var inboxChannelMessageProxyWebhook = await _webhookApi.CreateWebhookAsync(
                inboxChannel.Entity!.ID,
                $"{inboxChannel.Entity.Name.Value}Proxy",
                null);

            if (!inboxChannelMessageProxyWebhook.IsSuccess)
                return await RevertReportCreation(outboxChannel, outboxChannelMessageProxyWebhook, inboxChannel);

            await _mediator.Send(new AddReportRequest(_commandContext.User.ID.Value,
                outboxChannel.Entity.ID.Value,
                outboxChannelMessageProxyWebhook.Entity.ID.Value,
                outboxChannelMessageProxyWebhook.Entity.Token.Value!,
                inboxChannel.Entity.ID.Value,
                inboxChannelMessageProxyWebhook.Entity.ID.Value,
                inboxChannelMessageProxyWebhook.Entity.Token.Value!));

            await _commandResponder.Respond($"Report created, go to {outboxChannel.Entity.ID.ToChannelMention()}");

            await _channelApi.CreateMessageAsync(outboxChannel.Entity.ID, $"Start your report here {_commandContext.User.ID.ToUserMention()}");

            var user = _commandContext.User;

            var reportStatisticsForUser = await _mediator.Send(new GetUserReportsStatisticsForUserRequest(_commandContext.User.ID.Value));

            var userInfoPayload = new StringBuilder()
                .AppendLine("**User Information**")
                .AppendLine($"ID: {user.ID.Value}")
                .AppendLine($"Profile: {user.ID.ToUserMention()}")
                .AppendLine($"Handle: {DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator)}")
                .AppendLine($"Created: {DiscordSnowflakeHelper.ToDateTimeOffset(user.ID.Value):yyy-MM-dd HH:mm:ss}")
                .AppendLine()
                .AppendLine("**Report Statistics**")
                .AppendLine($"Previous reports: {reportStatisticsForUser}");

            var infoEmbed = new Embed()
            {
                Author = new EmbedAuthor(
                    DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator),
                    IconUrl: _avatarHelper.GetAvatarUrl(user)),
                Description = userInfoPayload.ToString(),
                Footer = new EmbedFooter("See logs for this user's reports via the /userreport logs command")
            };

            await _channelApi.CreateMessageAsync(inboxChannel.Entity.ID, $"{DiscordFormatter.RoleIdToMention(agentRoleId!.Value)}", embeds: new List<IEmbed> {infoEmbed});

            return Result.FromSuccess();
        }
        
        private async Task<IResult> RevertReportCreation(Result<IChannel> outboxChannel,
            Result<IWebhook> outboxChannelMessageProxyWebhook = default,
            Result<IChannel> inboxChannel = default,
            Result<IWebhook> inboxChannelMessageProxyWebhook = default)
        {
            if (outboxChannel.IsSuccess && outboxChannel.Entity != null)
            {
                await _channelApi.DeleteChannelAsync(outboxChannel.Entity!.ID);
            }

            if (outboxChannelMessageProxyWebhook.IsSuccess && outboxChannelMessageProxyWebhook.Entity != null)
            {
                await _webhookApi.DeleteWebhookAsync(outboxChannelMessageProxyWebhook.Entity!.ID);
            }

            if (inboxChannel.IsSuccess && inboxChannel.Entity != null)
            {
                await _channelApi.DeleteChannelAsync(inboxChannel.Entity!.ID);
            }

            if (inboxChannelMessageProxyWebhook.IsSuccess && inboxChannelMessageProxyWebhook.Entity != null)
            {
                await _webhookApi.DeleteWebhookAsync(inboxChannelMessageProxyWebhook.Entity!.ID);
            }

            return await _commandResponder.Respond("Report creation failed, please try again later.");
        }
        
    }
}
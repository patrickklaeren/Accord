using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private readonly CommandResponder _commandResponder;
        private readonly DiscordAvatarHelper _avatarHelper;
        private readonly DiscordCache _discordCache;
        private readonly IEventQueue _eventQueue;

        public ReportCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestGuildAPI guildApi,
            CommandResponder commandResponder,
            IDiscordRestChannelAPI channelApi,
            DiscordAvatarHelper avatarHelper,
            DiscordCache discordCache,
            IEventQueue eventQueue)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _guildApi = guildApi;
            _commandResponder = commandResponder;
            _channelApi = channelApi;
            _avatarHelper = avatarHelper;
            _discordCache = discordCache;
            _eventQueue = eventQueue;
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
                // TODO Handle existing report
            }
            else
            {
                var outboxCategoryId = await _mediator.Send(new GetUserReportsOutboxCategoryIdRequest());
                var inboxCategoryId = await _mediator.Send(new GetUserReportsInboxCategoryIdRequest());
                var agentRoleId = await _mediator.Send(new GetUserReportsAgentRoleIdRequest());

                if (agentRoleId is null)
                {
                    // TODO Figure out how to panic here
                    throw new InvalidOperationException("Cannot create report when there is no agent role");
                }

                var reporterPermissionOverwrite = new PermissionOverwrite(
                    _commandContext.User.ID,
                    PermissionOverwriteType.Member,
                    new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages),
                    new DiscordPermissionSet(BigInteger.Zero));

                var outboxChannel = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                    $"{_commandContext.User.Username}-{_commandContext.User.Discriminator.ToPaddedDiscriminator()}",
                    ChannelType.GuildText,
                    parentID: new Snowflake(outboxCategoryId!.Value),
                    permissionOverwrites: new[] { reporterPermissionOverwrite });

                if (!outboxChannel.IsSuccess)
                {
                    await _commandResponder.Respond("Failed creating report!");
                    return Result.FromError(outboxChannel.Error);
                }

                var inboxChannel = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                    $"{_commandContext.User.Username}-{_commandContext.User.Discriminator.ToPaddedDiscriminator()}-inbox",
                    ChannelType.GuildText,
                    parentID: new Snowflake(inboxCategoryId!.Value));

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
                    Author = new EmbedAuthor(DiscordHandleHelper.BuildHandle(user.Username, user.Discriminator),
                        IconUrl: _avatarHelper.GetAvatarUrl(user)),
                    Description = userInfoPayload.ToString(),
                    Footer = new EmbedFooter("See logs for this user's reports via the /userreport logs command")
                };

                await _channelApi.CreateMessageAsync(inboxChannel.Entity.ID, $"{DiscordFormatter.RoleIdToMention(agentRoleId!.Value)}", embed: infoEmbed);
            }

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), Command("reply"), Description("Reply to a user report, if in a user report channel")]
        public async Task<IResult> Reply(string message)
        {
            var response = await _mediator.Send(new GetIsUserReportsEnabledRequest());

            if (!response)
            {
                return Result.FromSuccess();
            }

            var userReportChannelType = await _mediator.Send(new GetUserReportChannelTypeRequest(_commandContext.ChannelID.Value));

            if (userReportChannelType == UserReportChannelType.None)
            {
                await _commandResponder.Respond("Channel is not a report channel");
                return Result.FromSuccess();
            }

            var member = await _discordCache.GetGuildMember(_commandContext.GuildID.Value.Value, _commandContext.User.ID.Value);

            if (!member.IsSuccess || member.Entity is null)
            {
                await _commandResponder.Respond("Failed finding user");
                return Result.FromSuccess();
            }

            var agentRoleId = await _mediator.Send(new GetUserReportsAgentRoleIdRequest());

            if (member.Entity.Roles.All(snowflake => snowflake.Value != agentRoleId))
            {
                await _commandResponder.Respond("You are not an agent!");
                return Result.FromSuccess();
            }

            var messageId = _commandContext is InteractionContext interaction
                ? interaction.ID.Value
                : _commandContext is MessageContext messageContext
                    ? messageContext.MessageID.Value
                    : throw new InvalidOperationException("Cannot determine message ID from unknown dispatch");

            await _eventQueue.Queue(new AddUserReportInboxMessageEvent(_commandContext.GuildID.Value.Value,
                messageId,
                _commandContext.User.ID.Value,
                _commandContext.ChannelID.Value,
                message,
                new List<DiscordAttachmentDto>(),
                DateTimeOffset.Now));

            return await _commandResponder.Respond(message);
        }
    }
}

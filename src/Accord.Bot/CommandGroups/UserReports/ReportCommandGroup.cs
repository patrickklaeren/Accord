using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Bot.Helpers;
using Accord.Services.Helpers;
using Accord.Services.UserReports;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups.UserReports;

public class ReportCommandGroup(ICommandContext commandContext, IMediator mediator, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi, IDiscordRestWebhookAPI webhookApi, FeedbackService feedbackService, DiscordAvatarHelper avatarHelper, DiscordCache discordCache) : AccordCommandGroup
{

    [Command("report"), Description("Start a user report")]
    public async Task<IResult> Report()
    {
        var response = await mediator.Send(new GetIsUserReportsEnabledRequest());

        if (!response)
        {
            return await feedbackService.SendContextualAsync("User reports are disabled!");
        }

        var proxy = commandContext.GetCommandProxy();

        var (hasExistingReport, outboxDiscordChannelId) = await mediator.Send(new GetExistingOutboxReportForUserRequest(proxy.UserId.Value));

        if (hasExistingReport && outboxDiscordChannelId is not null)
        {
            await feedbackService.SendContextualAsync(
                $"You have already an open report. Visit {DiscordFormatter.ChannelIdToMention(outboxDiscordChannelId.Value)} to continue with your report");
            await channelApi.CreateMessageAsync(new Snowflake(outboxDiscordChannelId.Value),
                $"{DiscordFormatter.UserIdToMention(proxy.UserId.Value)} Here!");
            return Result.FromSuccess();
        }

        var outboxCategoryId = await mediator.Send(new GetUserReportsOutboxCategoryIdRequest());
        var inboxCategoryId = await mediator.Send(new GetUserReportsInboxCategoryIdRequest());
        var agentRoleId = await mediator.Send(new GetUserReportsAgentRoleIdRequest());
        var everyoneRole = discordCache.GetEveryoneRole();

        if (agentRoleId is null)
        {
            // TODO Figure out how to panic here
            throw new InvalidOperationException("Cannot create report when there is no agent role");
        }

        var guildMember = await discordCache.GetGuildMember(proxy.UserId.Value);

        var everyonePermissionOverwrite = new PermissionOverwrite(
            everyoneRole.ID,
            PermissionOverwriteType.Role,
            new DiscordPermissionSet(BigInteger.Zero),
            new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages));

        var selfBotPermissionOverwrite = new PermissionOverwrite(
            discordCache.GetSelfSnowflake(),
            PermissionOverwriteType.Member,
            new DiscordPermissionSet(DiscordPermission.ViewChannel,
                DiscordPermission.SendMessages,
                DiscordPermission.ManageChannels,
                DiscordPermission.ManageWebhooks,
                DiscordPermission.ManageMessages),
            new DiscordPermissionSet(BigInteger.Zero));

        var reporterPermissionOverwrite = new PermissionOverwrite(
           proxy.UserId,
            PermissionOverwriteType.Member,
            new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages),
            new DiscordPermissionSet(BigInteger.Zero));

        var outboxChannel = await guildApi.CreateGuildChannelAsync(proxy.GuildId,
            guildMember.Entity.User.Value.Username,
            ChannelType.GuildText,
            parentID: new Snowflake(outboxCategoryId!.Value),
            permissionOverwrites: new[] {selfBotPermissionOverwrite, everyonePermissionOverwrite, reporterPermissionOverwrite});

        if (!outboxChannel.IsSuccess)
        {
            await feedbackService.SendContextualAsync("Failed creating report!");
            return Result.FromError(outboxChannel.Error);
        }

        var outboxChannelMessageProxyWebhook = await webhookApi.CreateWebhookAsync(
            outboxChannel.Entity!.ID,
            $"{outboxChannel.Entity.Name.Value}Proxy",
            null
        );

        if (!outboxChannelMessageProxyWebhook.IsSuccess)
            return await RevertReportCreation(outboxChannel);

        var inboxChannel = await guildApi.CreateGuildChannelAsync(proxy.GuildId,
            $"{guildMember.Entity.User.Value.Username}-inbox",
            ChannelType.GuildText,
            parentID: new Snowflake(inboxCategoryId!.Value));

        if (!inboxChannel.IsSuccess)
            return await RevertReportCreation(outboxChannel, outboxChannelMessageProxyWebhook);

        var inboxChannelMessageProxyWebhook = await webhookApi.CreateWebhookAsync(
            inboxChannel.Entity!.ID,
            $"{inboxChannel.Entity.Name.Value}Proxy",
            null);

        if (!inboxChannelMessageProxyWebhook.IsSuccess)
            return await RevertReportCreation(outboxChannel, outboxChannelMessageProxyWebhook, inboxChannel);

        await mediator.Send(new AddReportRequest(proxy.UserId.Value,
            outboxChannel.Entity.ID.Value,
            outboxChannelMessageProxyWebhook.Entity.ID.Value,
            outboxChannelMessageProxyWebhook.Entity.Token.Value!,
            inboxChannel.Entity.ID.Value,
            inboxChannelMessageProxyWebhook.Entity.ID.Value,
            inboxChannelMessageProxyWebhook.Entity.Token.Value!));

        await feedbackService.SendContextualAsync($"Report created, go to {outboxChannel.Entity.ID.ToChannelMention()}");

        await channelApi.CreateMessageAsync(outboxChannel.Entity.ID, $"Start your report here {proxy.UserId.ToUserMention()}");

        var reportStatisticsForUser = await mediator.Send(new GetUserReportsStatisticsForUserRequest(proxy.UserId.Value));

        var userInfoPayload = new StringBuilder()
            .AppendLine("**User Information**")
            .AppendLine($"ID: {proxy.UserId.Value}")
            .AppendLine($"Profile: {proxy.UserId.ToUserMention()}")
            .AppendLine($"Handle: {guildMember.Entity.User.Value.Username}")
            .AppendLine($"Created: {DiscordSnowflakeHelper.ToDateTimeOffset(proxy.UserId.Value):yyy-MM-dd HH:mm:ss}")
            .AppendLine()
            .AppendLine("**Report Statistics**")
            .AppendLine($"Previous reports: {reportStatisticsForUser}");

        var infoEmbed = new Embed()
        {
            Author = new EmbedAuthor(
                guildMember.Entity.User.Value.Username,
                IconUrl: avatarHelper.GetAvatarUrl(guildMember.Entity.User.Value.ID.Value,
                    guildMember.Entity.User.Value.Discriminator,
                    guildMember.Entity.User.Value.Avatar?.Value,
                    guildMember.Entity.User.Value.Avatar?.HasGif == true)),
            Description = userInfoPayload.ToString(),
            Footer = new EmbedFooter("See logs for this user's reports via the /userreport logs command")
        };

        await channelApi.CreateMessageAsync(inboxChannel.Entity.ID, $"{DiscordFormatter.RoleIdToMention(agentRoleId!.Value)}", embeds: new List<IEmbed> {infoEmbed});

        return Result.FromSuccess();
    }

    private async Task<IResult> RevertReportCreation(Result<IChannel> outboxChannel,
        Result<IWebhook> outboxChannelMessageProxyWebhook = default,
        Result<IChannel> inboxChannel = default,
        Result<IWebhook> inboxChannelMessageProxyWebhook = default)
    {
        if (outboxChannel.IsSuccess)
        {
            await channelApi.DeleteChannelAsync(outboxChannel.Entity!.ID);
        }

        if (outboxChannelMessageProxyWebhook is { IsSuccess: true, Entity: { } })
        {
            await webhookApi.DeleteWebhookAsync(outboxChannelMessageProxyWebhook.Entity!.ID);
        }

        if (inboxChannel is { IsSuccess: true, Entity: { } })
        {
            await channelApi.DeleteChannelAsync(inboxChannel.Entity!.ID);
        }

        if (inboxChannelMessageProxyWebhook is { IsSuccess: true, Entity: { } })
        {
            await webhookApi.DeleteWebhookAsync(inboxChannelMessageProxyWebhook.Entity!.ID);
        }

        return await feedbackService.SendContextualAsync("Report creation failed, please try again later.");
    }

}
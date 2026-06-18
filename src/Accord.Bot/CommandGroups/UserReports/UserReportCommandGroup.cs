using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
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

[Group("userreport")]
public class UserReportCommandGroup(ICommandContext commandContext, IMediator mediator, IDiscordRestGuildAPI guildApi, FeedbackService feedbackService, DiscordCache discordCache) : AccordCommandGroup
{
    [Command("setup"), Description("Sets up user reports for Guild usage")]
    public async Task<IResult> Setup()
    {
        var user = await commandContext.ToPermissionUser(guildApi);

        var response = await mediator.Send(new UserHasPermissionRequest(user, PermissionType.ManageUserReports));

        if (!response)
        {
            return await feedbackService.SendContextualAsync("Missing permission");
        }

        var isEnabled = await mediator.Send(new GetIsUserReportsEnabledRequest());

        if (!isEnabled)
        {
            return await feedbackService.SendContextualAsync("User reports is not enabled");
        }

        var proxy = commandContext.GetCommandProxy();

        var channelsInGuild = await guildApi.GetGuildChannelsAsync(proxy.GuildId);

        if (!channelsInGuild.IsSuccess)
        {
            await feedbackService.SendContextualAsync("Failed getting channels in Guild");
            return Result.FromError(channelsInGuild.Error!);
        }

        var rolesInGuild = await guildApi.GetGuildRolesAsync(proxy.GuildId);

        if (!rolesInGuild.IsSuccess)
        {
            await feedbackService.SendContextualAsync("Failed getting roles in Guild");
            return Result.FromError(channelsInGuild.Error!);
        }

        var roleSetup = await SetUpAgentRole(rolesInGuild.Entity);

        if (!roleSetup.IsSuccess)
        {
            return roleSetup;
        }

        var outboxSetup = await SetUpOutbox(channelsInGuild.Entity);

        if (!outboxSetup.IsSuccess)
        {
            return outboxSetup;
        }

        var inboxSetup = await SetUpInbox(channelsInGuild.Entity);

        if (!inboxSetup.IsSuccess)
        {
            return inboxSetup;
        }

        await feedbackService.SendContextualAsync("Set up!");

        return Result.FromSuccess();
    }

    private async Task<IResult> SetUpAgentRole(IReadOnlyList<IRole> rolesInGuild)
    {
        var proxy = commandContext.GetCommandProxy();

        var agentRoleId = await mediator.Send(new GetUserReportsAgentRoleIdRequest());

        if (agentRoleId is null
            || rolesInGuild.All(x => x.ID.Value != agentRoleId))
        {
            var roleCreation = await guildApi.CreateGuildRoleAsync(proxy.GuildId, "(Accord) User Reports Agent");

            if (!roleCreation.IsSuccess)
            {
                await feedbackService.SendContextualAsync("Failed creating role for agents");
                return Result.FromError(roleCreation.Error);
            }

            await mediator.Send(new UpdateRunOptionRequest(RunOptionType.UserReportsAgentRoleId, roleCreation.Entity.ID.Value.ToString()));
        }

        return Result.FromSuccess();
    }

    private async Task<IResult> SetUpOutbox(IReadOnlyList<IChannel> channelsInGuild)
    {
        var proxy = commandContext.GetCommandProxy();

        var userCategory = await mediator.Send(new GetUserReportsOutboxCategoryIdRequest());

        if (userCategory is null
            || channelsInGuild.All(x => x.ID.Value != userCategory))
        {
            var agentRoleId = await mediator.Send(new GetUserReportsAgentRoleIdRequest());

            var everyoneRole = discordCache.GetEveryoneRole();

            var selfBotPermissionOverwrite = new PermissionOverwrite(
                discordCache.GetSelfSnowflake(),
                PermissionOverwriteType.Member,
                new DiscordPermissionSet(DiscordPermission.ViewChannel,
                    DiscordPermission.SendMessages,
                    DiscordPermission.ManageChannels,
                    DiscordPermission.ManageWebhooks,
                    DiscordPermission.ManageMessages),
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
                new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages));

            var howToPermissionOverwrite = new PermissionOverwrite(
                everyoneRole.ID,
                PermissionOverwriteType.Role,
                new DiscordPermissionSet(DiscordPermission.ViewChannel),
                new DiscordPermissionSet(BigInteger.Zero));

            var categoryCreation = await guildApi.CreateGuildChannelAsync(proxy.GuildId,
                "User Reports",
                type: ChannelType.GuildCategory,
                permissionOverwrites: new[] {selfBotPermissionOverwrite, agentsPermissionOverwrite, everyonePermissionOverwrite});

            if (!categoryCreation.IsSuccess)
            {
                await feedbackService.SendContextualAsync("Failed creating category for outbox");
                return Result.FromError(categoryCreation.Error);
            }

            var howToChannelCreation = await guildApi.CreateGuildChannelAsync(proxy.GuildId,
                "how-to-report",
                ChannelType.GuildText,
                parentID: categoryCreation.Entity.ID,
                permissionOverwrites: new[] {howToPermissionOverwrite});

            if (!howToChannelCreation.IsSuccess)
            {
                await feedbackService.SendContextualAsync("Failed creating how-to-report channel under report under outbox category");
                return Result.FromError(howToChannelCreation.Error);
            }

            await mediator.Send(new UpdateRunOptionRequest(RunOptionType.UserReportsOutboxCategoryId, categoryCreation.Entity.ID.Value.ToString()));
        }

        return Result.FromSuccess();
    }

    private async Task<IResult> SetUpInbox(IReadOnlyList<IChannel> channelsInGuild)
    {
        var proxy = commandContext.GetCommandProxy();

        var inboxCategoryId = await mediator.Send(new GetUserReportsInboxCategoryIdRequest());

        var roleId = await mediator.Send(new GetUserReportsAgentRoleIdRequest());

        var everyoneRole = discordCache.GetEveryoneRole();

        if (inboxCategoryId is null
            || channelsInGuild.All(x => x.ID.Value != inboxCategoryId))
        {
            var selfBotPermissionOverwrite = new PermissionOverwrite(
                discordCache.GetSelfSnowflake(),
                PermissionOverwriteType.Member,
                new DiscordPermissionSet(DiscordPermission.ViewChannel,
                    DiscordPermission.SendMessages,
                    DiscordPermission.ManageChannels,
                    DiscordPermission.ManageMessages),
                new DiscordPermissionSet(BigInteger.Zero));

            var agentPermissionOverwrite = new PermissionOverwrite(
                new Snowflake(roleId!.Value),
                PermissionOverwriteType.Role,
                new DiscordPermissionSet(DiscordPermission.ViewChannel, DiscordPermission.SendMessages),
                new DiscordPermissionSet(BigInteger.Zero));

            var everyonePermissionOverwrite = new PermissionOverwrite(
                everyoneRole.ID,
                PermissionOverwriteType.Role,
                new DiscordPermissionSet(BigInteger.Zero),
                new DiscordPermissionSet(DiscordPermission.ViewChannel));

            var categoryCreation = await guildApi.CreateGuildChannelAsync(proxy.GuildId,
                "User Reports Inbox", type: ChannelType.GuildCategory,
                permissionOverwrites: new[] {selfBotPermissionOverwrite, agentPermissionOverwrite, everyonePermissionOverwrite});

            if (!categoryCreation.IsSuccess)
            {
                await feedbackService.SendContextualAsync("Failed creating category for inbox");
                return Result.FromError(categoryCreation.Error);
            }

            var howToChannelCreation = await guildApi.CreateGuildChannelAsync(proxy.GuildId,
                "inbox-how-to", ChannelType.GuildText, parentID: categoryCreation.Entity.ID);

            if (!howToChannelCreation.IsSuccess)
            {
                await feedbackService.SendContextualAsync("Failed creating how-to-report channel under report under inbox category");
                return Result.FromError(howToChannelCreation.Error);
            }

            await mediator.Send(new UpdateRunOptionRequest(RunOptionType.UserReportsInboxCategoryId, categoryCreation.Entity.ID.Value.ToString()));
        }

        return Result.FromSuccess();
    }
}

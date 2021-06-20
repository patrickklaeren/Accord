using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using Accord.Services.UserReports;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    [Group("userreport")]
    public class UserReportCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;

        public UserReportCommandGroup(ICommandContext commandContext,
            IMediator mediator,
            IDiscordRestWebhookAPI webhookApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestGuildAPI guildApi)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
            _guildApi = guildApi;
        }

        [RequireContext(ChannelContext.Guild), Command("setup"), Description("Sets up user reports for Guild usage")]
        public async Task<IResult> Setup()
        {
            var user = await _commandContext.ToPermissionUser(_guildApi);

            var response = await _mediator.Send(new UserHasPermissionRequest(user, PermissionType.ManageUserReports));

            if (!response)
            {
                await Respond("Missing permission");
                return Result.FromSuccess();
            }

            var isEnabled = await _mediator.Send(new GetIsUserReportsEnabledRequest());

            if (!isEnabled)
            {
                await Respond("User reports is not enabled");
                return Result.FromSuccess();
            }

            var channelsInGuild = await _guildApi.GetGuildChannelsAsync(_commandContext.GuildID.Value);

            if (!channelsInGuild.IsSuccess || channelsInGuild.Entity is null)
            {
                await Respond("Failed getting channels in Guild");
                return Result.FromError(channelsInGuild.Error!);
            }

            var userCategory = await _mediator.Send(new GetUserReportsOutboxCategoryIdRequest());

            if (userCategory is null 
                || channelsInGuild.Entity!.All(x => x.ID.Value != userCategory))
            {
                var result = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value, "User Reports", type: ChannelType.GuildCategory);

                if (!result.IsSuccess)
                {
                    await Respond("Failed creating category for outbox");
                    return Result.FromError(result.Error);
                }

                var howToChannelCreation = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value, 
                    "how-to-report", ChannelType.GuildText);

                if (!howToChannelCreation.IsSuccess)
                {
                    await Respond("Failed creating how-to-report channel under report under outbox category");
                    return Result.FromError(howToChannelCreation.Error);
                }

                await _mediator.Send(new UpdateRunOptionRequest(RunOptionType.UserReportsOutboxCategoryId, result.Entity.ID.Value.ToString()));
            }

            var moderatorCategory = await _mediator.Send(new GetUserReportsInboxCategoryIdRequest());

            if (moderatorCategory is null 
                || channelsInGuild.Entity!.All(x => x.ID.Value != moderatorCategory))
            {
                var result = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value, "User Reports Inbox", type: ChannelType.GuildCategory);

                if (!result.IsSuccess)
                {
                    await Respond("Failed creating category for inbox");
                    return Result.FromError(result.Error);
                }

                var howToChannelCreation = await _guildApi.CreateGuildChannelAsync(_commandContext.GuildID.Value,
                    "inbox-how-to", ChannelType.GuildText);

                if (!howToChannelCreation.IsSuccess)
                {
                    await Respond("Failed creating how-to-report channel under report under inbox category");
                    return Result.FromError(howToChannelCreation.Error);
                }

                await _mediator.Send(new UpdateRunOptionRequest(RunOptionType.UserReportsInboxCategoryId, result.Entity.ID.Value.ToString()));
            }

            await Respond("Set up!");

            return Result.FromSuccess();
        }

        private async Task Respond(string message)
        {
            if (_commandContext is InteractionContext interactionContext)
            {
                await _webhookApi.EditOriginalInteractionResponseAsync(interactionContext.ApplicationID, interactionContext.Token, content: message);
            }
            else
            {
                await _channelApi.CreateMessageAsync(_commandContext.ChannelID, content: message);
            }
        }
    }
}

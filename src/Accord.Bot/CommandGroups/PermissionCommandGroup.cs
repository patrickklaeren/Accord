using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups
{
    public class PermissionCommandGroup : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IMediator _mediator;
        private readonly IDiscordRestWebhookAPI _webhookApi;
        private readonly IDiscordRestChannelAPI _channelApi;

        public PermissionCommandGroup(ICommandContext commandContext,
            IMediator mediator, 
            IDiscordRestWebhookAPI webhookApi, 
            IDiscordRestChannelAPI channelApi)
        {
            _commandContext = commandContext;
            _mediator = mediator;
            _webhookApi = webhookApi;
            _channelApi = channelApi;
        }

        [RequireContext(ChannelContext.Guild), RequireUserGuildPermission(DiscordPermission.Administrator), Command("perm-to-user"), Description("Add permission to a user")]
        public async Task<IResult> AddPermissionToMember(IGuildMember member, string type)
        {
            if (!Enum.TryParse<PermissionType>(type, out var actualPermission) || !Enum.IsDefined(actualPermission))
            {
                await Respond("Permission is not found");
            }
            else if(member.User.HasValue)
            {
                await _mediator.Send(new AddPermissionForUserRequest(member.User.Value.ID.Value, actualPermission));
                await Respond($"{actualPermission} permission added to {member.User.Value.ID.ToUserMention()}");
            }

            return Result.FromSuccess();
        }

        [RequireContext(ChannelContext.Guild), RequireUserGuildPermission(DiscordPermission.Administrator), Command("perm-to-role"), Description("Add permission to a role")]
        public async Task<IResult> AddPermissionToRole(IRole role, string type)
        {
            if (!Enum.TryParse<PermissionType>(type, out var actualPermission) || !Enum.IsDefined(actualPermission))
            {
                await Respond("Permission is not found");
            }
            else
            {
                await _mediator.Send(new AddPermissionForRoleRequest(role.ID.Value, actualPermission));
                await Respond($"{actualPermission} permission added to `{role.Name}`");
            }

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

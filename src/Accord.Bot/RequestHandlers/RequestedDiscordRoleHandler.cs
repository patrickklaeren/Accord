using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.TimedRoles;
using MediatR;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Accord.Bot.RequestHandlers;

public class RequestedDiscordRoleHandler(IDiscordRestGuildAPI guildApi, DiscordConfiguration configuration)
    : IRequestHandler<GrantRequestedDiscordRoleRequest, ServiceResponse>,
      IRequestHandler<CheckRequestedDiscordRoleRequest, ServiceResponse>,
      IRequestHandler<RevokeRequestedDiscordRoleRequest, bool>
{
    public async Task<ServiceResponse> Handle(CheckRequestedDiscordRoleRequest request, CancellationToken ct)
    {
        var guildId = new Snowflake(configuration.GuildId);
        var userId = new Snowflake(request.DiscordUserId);
        var roleId = new Snowflake(request.DiscordRoleId);
        var member = await guildApi.GetGuildMemberAsync(guildId, userId, ct);
        if (!member.IsSuccess)
            return ServiceResponse.Fail("Unable to find you in the server. Run /get-role again.");
        // Never turn an existing permanent role into a temporary grant.
        if (member.Entity.Roles.Contains(roleId))
            return ServiceResponse.Fail("You already have this role.");

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse> Handle(GrantRequestedDiscordRoleRequest request, CancellationToken ct)
    {
        var response = await guildApi.AddGuildMemberRoleAsync(new Snowflake(configuration.GuildId),
            new Snowflake(request.DiscordUserId), new Snowflake(request.DiscordRoleId),
            "Requested role: 30-minute grant", ct);
        return response.IsSuccess ? ServiceResponse.Ok()
            : ServiceResponse.Fail("Unable to assign the role. Run /get-role again or contact an administrator.");
    }

    public async Task<bool> Handle(RevokeRequestedDiscordRoleRequest request, CancellationToken ct)
    {
        var response = await guildApi.RemoveGuildMemberRoleAsync(new Snowflake(configuration.GuildId),
            new Snowflake(request.DiscordUserId), new Snowflake(request.DiscordRoleId),
            "Requested role expired after 30 minutes", ct);
        return response.IsSuccess;
    }
}

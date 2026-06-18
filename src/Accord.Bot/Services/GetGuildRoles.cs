using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using MediatR;

namespace Accord.Bot.Services;

public sealed record GetGuildRolesRequest : IRequest<ServiceResponse<IEnumerable<DiscordGuildRoleDto>>>;
public record DiscordGuildRoleDto(ulong DiscordRoleId, string Name);

public class GetGuildRolesHandler(DiscordCache discordCache) : IRequestHandler<GetGuildRolesRequest, ServiceResponse<IEnumerable<DiscordGuildRoleDto>>>
{

    public async Task<ServiceResponse<IEnumerable<DiscordGuildRoleDto>>> Handle(GetGuildRolesRequest request, CancellationToken cancellationToken)
    {
        var roles = await discordCache.GetGuildRoles();

        if(!roles.Any())
        {
            return ServiceResponse.Fail<IEnumerable<DiscordGuildRoleDto>>("No roles");
        }

        var projected = roles
            .OrderBy(x => x.Name)
            .Select(x => new DiscordGuildRoleDto(x.ID.Value, x.Name));

        return ServiceResponse.Ok(projected);
    }
}
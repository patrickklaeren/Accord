using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using MediatR;

namespace Accord.Bot.Services;

public sealed record GetGuildMemberRequest(ulong DiscordUserId) : IRequest<ServiceResponse<DiscordGuildMemberDto>>;
public record DiscordGuildMemberDto(string Username, string? Nickname, string? GuildAvatarHash, ulong[] RoleIds);

[AutoConstructor]
public partial class GetGuildMemberHandler : IRequestHandler<GetGuildMemberRequest, ServiceResponse<DiscordGuildMemberDto>>
{
    private readonly DiscordCache _discordCache;

    public async Task<ServiceResponse<DiscordGuildMemberDto>> Handle(GetGuildMemberRequest request, CancellationToken cancellationToken)
    {
        var discordMember = await _discordCache.GetGuildMember(request.DiscordUserId);

        if (!discordMember.IsSuccess)
        {
            return ServiceResponse.Fail<DiscordGuildMemberDto>("No way");
        }

        var member = discordMember.Entity;

        var dto = new DiscordGuildMemberDto(member.User.Value.Username, 
            member.Nickname.Value, 
            member.Avatar.Value?.Value, 
            member.Roles.Select(d => d.Value).ToArray());

        return ServiceResponse.Ok(dto);
    }
}
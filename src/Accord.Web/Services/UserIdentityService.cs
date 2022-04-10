using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Accord.Services.Users;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;

namespace Accord.Web.Services;

public class UserIdentityService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IMediator _mediator;

    public UserIdentityService(AuthenticationStateProvider authenticationStateProvider, IMediator mediator)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _mediator = mediator;
    }

    public async Task<bool> IsPartOfGuild()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if (state.User.Identity is null || state.User.Identity?.IsAuthenticated == false)
            return false;

        var nameClaim = state
            .User
            .Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

        if (ulong.TryParse(nameClaim?.Value, out var discordUserId))
        {
            return await _mediator.Send(new UserExistsRequest(discordUserId));
        }

        return false;
    }
}
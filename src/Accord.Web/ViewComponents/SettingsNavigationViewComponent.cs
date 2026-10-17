using System;
using System.Threading.Tasks;
using Accord.Services.Guilds;
using Accord.Web.DiscordServices;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Accord.Web.ViewComponents;

public sealed class SettingsNavigationViewComponent(IMediator mediator, DiscordUserService currentUser,
    ILogger<SettingsNavigationViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Content(string.Empty);

        try
        {
            var access = ViewContext.ViewData.Model as GuildSettingsAccessResponse
                ?? await mediator.Send(new GetGuildSettingsAccessRequest(currentUser.GetDiscordId()), HttpContext.RequestAborted);
            return access.CanAccessSettings ? View() : Content(string.Empty);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not load settings navigation access");
            return Content(string.Empty);
        }
    }
}

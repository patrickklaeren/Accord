using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services;
using Accord.Services.Guilds;
using Accord.Services.Permissions;
using Accord.Web.DiscordServices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accord.Web.Controllers;

[Authorize]
public abstract class AccordController(IMediator mediator, DiscordUserService currentUser) : Controller
{
    protected IMediator Mediator { get; } = mediator;
    protected DiscordUserService CurrentUser { get; } = currentUser;

    protected async Task<PermissionUser?> GetPermissionUser(CancellationToken ct) =>
        (await Mediator.Send(new GetGuildAccessRequest(CurrentUser.GetDiscordId()), ct)).User;

    protected Task<bool> HasPermission(PermissionUser? user, PermissionType permission, CancellationToken ct) =>
        user is null ? Task.FromResult(false) : Mediator.Send(new UserHasPermissionRequest(user, permission), ct);

    protected IActionResult AccessDenied(string message)
    {
        if (WantsJson)
            return StatusCode(403, new { message });
        Response.StatusCode = 403;
        return View("AccessDenied", message);
    }

    private bool WantsJson => Request.Headers.Accept.Any(value => value?.Contains("application/json") == true);

    protected IActionResult Saved(ServiceResponse result, string action, string successMessage, object? routeValues = null)
    {
        if (WantsJson)
            return result.Success ? Ok(new { message = successMessage }) : BadRequest(new { message = result.ErrorMessage });

        TempData[result.Success ? "Success" : "Error"] = result.Success ? successMessage : result.ErrorMessage;
        return RedirectToAction(action, routeValues);
    }

    protected IActionResult InvalidForm(string action, object? routeValues = null) =>
        Saved(ServiceResponse.Fail(string.Join(" ", ModelState.Values.SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "Check the submitted values." : error.ErrorMessage))), action, string.Empty, routeValues);
}

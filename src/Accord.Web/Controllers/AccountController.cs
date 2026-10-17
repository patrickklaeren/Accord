using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accord.Web.Controllers;

public sealed class AccountController : Controller
{
    [AllowAnonymous, HttpGet("/login")]
    public IActionResult Login() => Challenge(new AuthenticationProperties { RedirectUri = "/dashboard" },
        DiscordAuthenticationDefaults.AuthenticationScheme);

    [Authorize, HttpPost("/logout")]
    public IActionResult Logout() => SignOut(new AuthenticationProperties { RedirectUri = "/" },
        CookieAuthenticationDefaults.AuthenticationScheme);
}

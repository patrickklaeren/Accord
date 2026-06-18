using System;
using System.Security.Claims;
using Accord.Services.Helpers;
using Microsoft.AspNetCore.Http;

namespace Accord.Web.DiscordServices;

[RegisterScoped]
public class DiscordUserService(IHttpContextAccessor httpContextAccessor, DiscordAvatarHelper discordAvatarHelper)
{

    public string GetAvatarUrl()
    {
        if (httpContextAccessor.HttpContext is not { User.Identity.IsAuthenticated: true } context)
        {
            throw new InvalidOperationException("Cannot get avatar for user when they are not authenticated");
        }

        var discordUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var discriminator = context.User.FindFirstValue("urn:discord:user:discriminator")!;
        var avatarClaim = context.User.FindFirstValue("urn:discord:avatar:hash");

        return discordAvatarHelper.GetAvatarUrl(ulong.Parse(discordUserId),
            ushort.Parse(discriminator),
            avatarClaim);
    }

    public ulong GetDiscordId()
    {
        if (httpContextAccessor.HttpContext is not { User.Identity.IsAuthenticated: true } context)
        {
            throw new InvalidOperationException("Cannot get ID for user when they are not authenticated");
        }

        var discordUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return ulong.Parse(discordUserId);
    }
}

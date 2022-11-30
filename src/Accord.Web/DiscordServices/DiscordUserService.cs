﻿using Accord.Services.Helpers;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Accord.Web.Services;

[AutoConstructor, Inject(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)]
public partial class DiscordUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DiscordAvatarHelper _discordAvatarHelper;

    public string GetAvatarUrl()
    {
        if (!(_httpContextAccessor.HttpContext is { User.Identity.IsAuthenticated: true } context))
        {
            throw new InvalidOperationException("Cannot get avatar for user when they are not authenticated");
        }

        var discordUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var discriminator = context.User.FindFirstValue("urn:discord:user:discriminator")!;
        var avatarClaim = context.User.FindFirstValue("urn:discord:avatar:hash");

        return _discordAvatarHelper.GetAvatarUrl(ulong.Parse(discordUserId),
            ushort.Parse(discriminator),
            avatarClaim);
    }

    public ulong GetDiscordId()
    {
        if (!(_httpContextAccessor.HttpContext is { User.Identity.IsAuthenticated: true } context))
        {
            throw new InvalidOperationException("Cannot get ID for user when they are not authenticated");
        }

        var discordUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return ulong.Parse(discordUserId);
    }
}

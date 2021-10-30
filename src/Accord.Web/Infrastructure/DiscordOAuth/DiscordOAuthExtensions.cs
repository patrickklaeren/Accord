using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Accord.Web.Infrastructure.DiscordOAuth;

public static class DiscordOAuthExtensions
{
    public static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder, Action<DiscordOAuthOptions> configureOptions)
        => builder.AddDiscord(DiscordOAuthConstants.AUTHENTICATION_SCHEME, configureOptions);

    private static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder, string authenticationScheme, Action<DiscordOAuthOptions> configureOptions)
        => builder.AddDiscord(authenticationScheme, DiscordOAuthConstants.DISPLAY_NAME, configureOptions);

    private static AuthenticationBuilder AddDiscord(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<DiscordOAuthOptions> configureOptions)
        => builder.AddOAuth<DiscordOAuthOptions, DiscordOAuthHandler>(authenticationScheme, displayName, configureOptions);
}
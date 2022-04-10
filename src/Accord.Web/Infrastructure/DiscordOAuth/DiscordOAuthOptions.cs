using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Accord.Web.Infrastructure.DiscordOAuth;

public class DiscordOAuthOptions : OAuthOptions
{
    public DiscordOAuthOptions()
    {
        CallbackPath = new PathString("/signin-discord");
        AuthorizationEndpoint = DiscordOAuthConstants.AUTHORIZATION_ENDPOINT;
        TokenEndpoint = DiscordOAuthConstants.TOKEN_ENDPOINT;
        UserInformationEndpoint = DiscordOAuthConstants.USER_INFORMATION_ENDPOINT;
        Scope.Add("identify");

        ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id", ClaimValueTypes.UInteger64);
        ClaimActions.MapJsonKey(ClaimTypes.Name, "username", ClaimValueTypes.String);
        ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
        ClaimActions.MapJsonKey("urn:discord:discriminator", "discriminator", ClaimValueTypes.UInteger32);
        ClaimActions.MapJsonKey("urn:discord:avatar", "avatar", ClaimValueTypes.String);
        ClaimActions.MapJsonKey("urn:discord:verified", "verified", ClaimValueTypes.Boolean);
    }
}
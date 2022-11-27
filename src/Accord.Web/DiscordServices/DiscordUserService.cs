using Accord.Bot.Infrastructure;
using Accord.Services.Helpers;
using LazyCache;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Accord.Web.Services;

[AutoConstructor, Inject(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)]
public partial class DiscordUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DiscordAvatarHelper _discordAvatarHelper;
    private readonly HttpClient _httpClient;
    private readonly IAppCache _appCache;

    [AutoConstructorInject("discordConfiguration.Value", "discordConfiguration", typeof(IOptions<DiscordConfiguration>))]
    private readonly DiscordConfiguration _discordConfiguration;

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

    public async Task<string> GetUsersNicknameInGuild()
    {
        if (_httpContextAccessor.HttpContext is not { User.Identity.IsAuthenticated: true } context)
        {
            throw new InvalidOperationException("Cannot get nickname when they are not authenticated");
        }

        var discordUserName = context.User.FindFirstValue(ClaimTypes.Name)!;

        var userInGuild = await GetUserAsGuildUser();
        return userInGuild?.Nickname ?? discordUserName;
    }

    public async Task<DiscordGuildMemberDto?> GetUserAsGuildUser()
    {
        return await _appCache.GetOrAddAsync(nameof(GetUserAsGuildUser), GetUserAsGuildUserData);

        async Task<DiscordGuildMemberDto?> GetUserAsGuildUserData()
        {
            if (_httpContextAccessor.HttpContext is not { User.Identity.IsAuthenticated: true } context)
            {
                throw new InvalidOperationException("Cannot get member for user when they are not authenticated");
            }

            var accessToken = await context.GetTokenAsync("access_token");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            return await _httpClient.GetFromJsonAsync<DiscordGuildMemberDto>($"https://discordapp.com/api/users/@me/guilds/{_discordConfiguration.GuildId}/member");
        }
    }
}

public record DiscordGuildMemberDto(
    [property: JsonPropertyName("nick")] string? Nickname, 
    [property: JsonPropertyName("avatar")] string? GuildAvatarHash, 
    [property: JsonPropertyName("roles")] ulong[] RoleIds);

using Accord.Bot.Infrastructure;

namespace Accord.Services.Helpers;

[AutoConstructor]
public partial class DiscordAvatarHelper
{
    private readonly DiscordConfiguration _discordConfiguration;

    public string GetAvatarUrl(ulong discordUserId, 
        ushort discordDiscriminator, 
        string? avatarHash,
        bool showAsGif = false)
    {
        if (string.IsNullOrWhiteSpace(avatarHash))
        {
            var resultModulus = discordDiscriminator % 5;
            return $"{_discordConfiguration.CdnBaseUrl}/embed/avatars/{resultModulus}.png";
        }

        var extension = "png";

        if (showAsGif)
        {
            extension = "gif";
        }

        return $"{_discordConfiguration.CdnBaseUrl}/avatars/{discordUserId}/{avatarHash}.{extension}";
    }
}
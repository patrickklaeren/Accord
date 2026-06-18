namespace Accord.Services.Helpers;

[RegisterSingleton]
public class DiscordAvatarHelper(DiscordConfiguration discordConfiguration)
{

    public string GetAvatarUrl(ulong discordUserId,
        ushort discordDiscriminator,
        string? avatarHash,
        bool showAsGif = false)
    {
        if (string.IsNullOrWhiteSpace(avatarHash))
        {
            var resultModulus = discordDiscriminator % 5;
            return $"{discordConfiguration.CdnBaseUrl}/embed/avatars/{resultModulus}.png";
        }

        var extension = "png";

        if (showAsGif)
        {
            extension = "gif";
        }

        return $"{discordConfiguration.CdnBaseUrl}/avatars/{discordUserId}/{avatarHash}.{extension}";
    }
}

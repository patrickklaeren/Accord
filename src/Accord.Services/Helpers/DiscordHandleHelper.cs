namespace Accord.Services.Helpers;

public static class DiscordHandleHelper
{
    public static string BuildHandle(string username, string discriminator)
    {
        return $"{username}#{discriminator}";
    }

    public static string BuildHandle(string username, ulong discriminator)
    {
        return discriminator is 0 ? username : BuildHandle(username, discriminator.ToString("0000"));
    }
}
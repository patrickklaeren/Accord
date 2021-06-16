namespace Accord.Services.Helpers
{
    public static class DiscordHandleHelper
    {
        public static string BuildHandle(string username, string discriminator)
        {
            return $"{username}#{discriminator}";
        }
    }
}

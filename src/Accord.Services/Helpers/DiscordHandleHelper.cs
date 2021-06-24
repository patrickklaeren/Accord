namespace Accord.Services.Helpers
{
    public static class DiscordHandleHelper
    {
        public static string BuildHandle(string username, string discriminator)
        {
            return $"{username}#{discriminator}";
        }

        public static string BuildHandle(string username, ulong discriminator)
        {
            return BuildHandle(username, discriminator.ToString("0000"));
        }

        public static (string username, string discriminator) DeconstructHandle(string handle)
        {
            var split = handle.Split('#');
            return (split[0], split[1]);
        }
    }
}

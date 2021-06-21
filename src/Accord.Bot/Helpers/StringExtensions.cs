namespace Accord.Bot.Helpers
{
    public static class StringExtensions
    {
        private static readonly string[] SpecialCharacters = { "\\", "`", "|" };
        public static string DiscordSanitize(this string text)
        {
            foreach (string c in SpecialCharacters)
            {
                text = text.Replace(c, $"\\{c}");
            }
            return text;
        }
    }
}
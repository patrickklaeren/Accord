namespace Accord.Bot.Extensions
{
    public static class StringExtensions
    {
        private static readonly string[] SpecialCharacters = { "\\", "`", "|" };

        public static string DiscordSanitize(this string text)
        {
            foreach (var character in SpecialCharacters)
            {
                text = text.Replace(character, $"\\{character}");
            }
            return text;
        }
    }
}
namespace Accord.Bot.Extensions;

public static class StringExtensions
{
    private static readonly string[] SpecialCharacters = ["\\", "`", "|"];

    extension(string text)
    {
        public string SanitiseDiscordContent()
        {
            foreach (var character in SpecialCharacters)
            {
                text = text.Replace(character, $"\\{character}");
            }
            return text;
        }
    }
}
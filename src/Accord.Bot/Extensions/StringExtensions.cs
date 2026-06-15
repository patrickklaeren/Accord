using System;
using System.Text;

namespace Accord.Bot.Extensions;

public static class StringExtensions
{
    private static readonly string[] SpecialCharacters = { "\\", "`", "|" };

    extension(string text)
    {
        public string DiscordSanitize()
        {
            foreach (var character in SpecialCharacters)
            {
                text = text.Replace(character, $"\\{character}");
            }
            return text;
        }

        public string UnquoteAgentReportText()
        {
            var lines = text.Split(Environment.NewLine);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line[0] == '>' ? line[1..].Trim() : line.Trim());
            }

            return sb.ToString();
        }
    }
}
using System;
using System.Text;

namespace Accord.Bot.Extensions;

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

    public static string UnquoteAgentReportText(this string text)
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
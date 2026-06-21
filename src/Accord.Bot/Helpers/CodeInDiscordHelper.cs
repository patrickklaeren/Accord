using System;
using System.Text.RegularExpressions;

namespace Accord.Bot.Helpers;

public static partial class CodeInDiscordHelper
{
    [GeneratedRegex(@"```([^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CodeBlockStripperRegex();

    public static string Sanitise(string input)
    {
        var sanitised = input.Trim();
        return CodeBlockStripperRegex().Replace(sanitised, string.Empty);
    }
    
    [GeneratedRegex("^", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex ExceptionMadeNiceForDiscordEmbedRegex();

    public static string MakeRawExceptionNiceForDiscordEmbed(string rawException)
    {
        return ExceptionMadeNiceForDiscordEmbedRegex().Replace(rawException, "- ");
    }

    public static string FormatAsCodeBlock(string input, string language = "cs")
    {
        return string.IsNullOrWhiteSpace(input)
            ? $"```{Environment.NewLine}```"
            : $"```{language}{Environment.NewLine}{input}{Environment.NewLine}```";
    }
    
    public static string TruncateToEmbedField(string input)
    {
        const int MAX_FIELD_LENGTH = 252;
        
        return input.Length < MAX_FIELD_LENGTH
            ? input
            : input[..MAX_FIELD_LENGTH] + "...";
    }
}
using System;
using System.Text.RegularExpressions;
using Remora.Rest.Core;

namespace Accord.Bot.Helpers;

public static partial class DiscordFormatter
{
    // https://discord.com/developers/docs/reference#message-formatting

    public static string UserIdToMention(ulong discordUserId) => $"<@{discordUserId}>";
    public static string UserIdToMention(string discordUserId) => $"<@{discordUserId}>";
    public static string ChannelIdToMention(ulong discordChannelId) => $"<#{discordChannelId}>";
    public static string RoleIdToMention(ulong discordRoleId) => $"<@&{discordRoleId}>";
    public static string TimeToMarkdown(DateTimeOffset dateTimeOffset, TimeToMentionType type = TimeToMentionType.ShortDateTime) =>
        $"<t:{dateTimeOffset.ToUnixTimeSeconds()}:{(char)type}>";
    public static string TimeToMarkdown(DateTime dateTime, TimeToMentionType type = TimeToMentionType.ShortDateTime) => TimeToMarkdown(dateTimeOffset: dateTime, type);

    public static string ToUserMention(this Snowflake snowflake) => UserIdToMention(snowflake.Value);
    public static string ToRoleMention(this Snowflake snowflake) => RoleIdToMention(snowflake.Value);
    public static string ToChannelMention(this Snowflake snowflake) => ChannelIdToMention(snowflake.Value);
    public static string ToTimeMarkdown(this DateTime dateTime, TimeToMentionType type = TimeToMentionType.ShortDateTime) => TimeToMarkdown(dateTime, type);
    public static string ToTimeMarkdown(this DateTimeOffset dateTimeOffset, TimeToMentionType type = TimeToMentionType.ShortDateTime) => TimeToMarkdown(dateTimeOffset, type);

    public static string ToSmallMarkdown(string input) => $"-# {input}";
    
    [GeneratedRegex(@"```([^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CodeBlockStripperRegex();

    public static string StripCodeBlocks(string input)
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

    [GeneratedRegex(@"\|\|.+\|\|", RegexOptions.Compiled)]
    private static partial Regex ContainsSpoilerRegex();
    public static bool ContainsSpoiler(string text) => ContainsSpoilerRegex().IsMatch(text);
}
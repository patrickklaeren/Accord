using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Accord.Bot.Extensions;
using Accord.Services.Tags;
using MediatR;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public partial class TagHelper(IMediator mediator)
{
    [GeneratedRegex(@"\$(\S+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private partial Regex InlineTagRegex();

    [GeneratedRegex("^>.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
    private partial Regex MessageQuoteRegex();

    public async Task<string[]> TryGetTags(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var sanitised = content
            .Trim()
            .SanitiseDiscordContent();

        sanitised = sanitised.StripCode();
        sanitised = MessageQuoteRegex().Replace(sanitised, string.Empty);

        if (string.IsNullOrWhiteSpace(sanitised))
            return [];

        var matches = InlineTagRegex().Matches(sanitised);

        if (matches.Count == 0)
            return [];

        var tagNames = matches.Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return await mediator.Send(new GetTagsContentsRequest(tagNames));
    }
}
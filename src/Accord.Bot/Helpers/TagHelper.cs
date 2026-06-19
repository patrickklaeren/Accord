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

    public async Task<string?> TryGetTag(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
        
        var sanitised = content
            .Trim()
            .SanitiseDiscordContent();

        sanitised = MessageQuoteRegex().Replace(sanitised, string.Empty);

        if (string.IsNullOrWhiteSpace(sanitised))
            return null;

        var matches = InlineTagRegex().Match(sanitised);

        if (!matches.Success)
            return null;

        var tagName = matches.Groups[1].Value;

        return await mediator.Send(new GetTagContentRequest(tagName));
    }
}
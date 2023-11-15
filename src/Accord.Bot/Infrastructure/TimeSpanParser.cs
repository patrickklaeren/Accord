using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeSpanParserUtil;

namespace Accord.Bot.Infrastructure;

public class TimeSpanParser : AbstractTypeParser<TimeSpan>
{
    public override ValueTask<Result<TimeSpan>> TryParseAsync(string value, CancellationToken ct = default)
    {
        return new ValueTask<Result<TimeSpan>>(
            TimeSpanParserUtil.TimeSpanParser.TryParse(
                value.ToLowerInvariant(),
                new TimeSpanParserOptions { DecimalSecondsCountsAsMilliseconds = true }, out var timeSpan)
                ? Result<TimeSpan>.FromSuccess(timeSpan)
                : new ParsingError<TimeSpan>($"Could not parse input \"{value}\" into a valid {nameof(TimeSpanParserUtil.TimeSpanParser)}")
        );
    }
}

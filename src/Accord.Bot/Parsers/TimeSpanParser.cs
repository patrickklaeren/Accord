using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;
using TimeSpanParserUtil;

namespace Accord.Bot.Parsers
{
    /// <summary>
    /// Parses instances of <see cref="TimeSpanParser"/>.
    /// </summary>
    public class TimeSpanParser : AbstractTypeParser<TimeSpan>
    {
        /// <inheritdoc />
        public override ValueTask<Result<TimeSpan>> TryParse(string value, CancellationToken ct)
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
}
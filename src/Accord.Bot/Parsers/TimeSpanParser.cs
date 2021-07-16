using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;

namespace Accord.Bot.Parsers
{
    /// <summary>
    /// Parses instances of <see cref="TimeSpanParser"/>.
    /// </summary>
    public class TimeSpanParser : AbstractTypeParser<TimeSpan>
    {
        private static readonly string[] Formats =
        {
            "%d'd'%h'h'%m'm'%s's'", // 4d3h2m1s
            "%d'd'%h'h'%m'm'",      // 4d3h2m
            "%d'd'%h'h'%s's'",      // 4d3h1s
            "%d'd'%h'h'",           // 4d3h
            "%d'd'%m'm'%s's'",      // 4d2m1s
            "%d'd'%m'm'",           // 4d2m
            "%d'd'%s's'",           // 4d1s
            "%d'd'",                // 4d
            "%h'h'%m'm'%s's'",      // 3h2m1s
            "%h'h'%m'm'",           // 3h2m
            "%h'h'%s's'",           // 3h1s
            "%h'h'",                // 3h
            "%m'm'%s's'",           // 2m1s
            "%m'm'",                // 2m
            "%s's'",                // 1s
        };
        
        /// <inheritdoc />
        public override ValueTask<Result<TimeSpan>> TryParse(string value, CancellationToken ct)
        {
            return new(
                TimeSpan.TryParseExact(value.ToLowerInvariant(), Formats, CultureInfo.InvariantCulture,
                    out var timeSpan)
                    ? Result<TimeSpan>.FromSuccess(timeSpan)
                    : new ParsingError<TimeSpan>(
                        $"Could not parse input \"{value}\" into a valid {nameof(TimeSpan)}")
            );
        }
    }
}
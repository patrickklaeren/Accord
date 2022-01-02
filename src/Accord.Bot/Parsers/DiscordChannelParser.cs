using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Extensions;
using Remora.Rest.Core;
using Remora.Results;

namespace Accord.Bot.Parsers;

public record ChannelParsingResults(IReadOnlyList<IChannel> Channels);
public record ApproximateChannelParsingResults(IReadOnlyList<IChannel> Channels, Dictionary<IChannel, int> Scores) : ChannelParsingResults(Channels);

public class DiscordChannelParser : AbstractTypeParser<IChannel>
{
    private readonly DiscordScopedCache _discordCache;

    public DiscordChannelParser(DiscordScopedCache discordCache)
    {
        _discordCache = discordCache;
    }

    public Result<ChannelParsingResults> TryParseChannelFromId(string value, IReadOnlyList<IChannel> channelList)
    {
        if (!ulong.TryParse(value, out var channelId))
            return new ParsingError<IChannel>(value);

        var result = channelList.SingleOrDefault(x => x.ID.Value == channelId);
        return result != null
            ? new ChannelParsingResults(new[] {result})
            : new ParsingError<IChannel>(value);
    }

    public Result<ChannelParsingResults> TryParseChannelFromSnowflake(string value, IReadOnlyList<IChannel> channelList)
    {
        if (!Snowflake.TryParse(value.Unmention(), out var channelId))
        {
            return new ParsingError<IChannel>(value.Unmention());
        }

        var result = channelList.SingleOrDefault(x => x.ID.Value == channelId.Value.Value);
        return result != null
            ? new ChannelParsingResults(new[] {result})
            : new ParsingError<IChannel>(value);
    }

    public Result<ChannelParsingResults> TryParseChannelFromName(string value, IReadOnlyList<IChannel> channelList)
    {
        var channels = channelList
            .Where(x => string.Equals(x.Name.Value!.Replace("#", ""), value.Replace("#", ""), StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        if (channels.Count == 0)
            return new ParsingError<IChannel>(value);

        return new ChannelParsingResults(channels);
    }

    public Result<ApproximateChannelParsingResults> TryParseChannelFromNameLevenshtein(string value, IReadOnlyList<IChannel> channelList)
    {
        var channels = channelList
            .Select(x => new
            {
                Channel = x,
                Score = value
                    .ToLowerInvariant()
                    .Replace("#", "")
                    .GetDamerauLevenshteinDistance(x.Name.Value!.ToLowerInvariant().Replace("#", ""))
            })
            .OrderBy(x => x.Score)
            .Where(x => x.Score <= 2)
            .ToDictionary(x => x.Channel, x => x.Score);
        if (channels.Count == 0)
            return new ParsingError<IChannel>(value);


        return new ApproximateChannelParsingResults(channels.Keys.ToList(), channels);
    }

    public Result<ChannelParsingResults> TryParse(string value, bool allowApproximate = true)
    {
        Result<ChannelParsingResults> channelResult;
        var guildChannels = _discordCache.GetGuildChannels();

        if ((channelResult = TryParseChannelFromId(value, guildChannels)).IsSuccess)
            return channelResult.Entity;

        if ((channelResult = TryParseChannelFromSnowflake(value, guildChannels)).IsSuccess)
            return channelResult.Entity;

        if ((channelResult = TryParseChannelFromName(value, guildChannels)).IsSuccess)
            return channelResult.Entity;

        if (!allowApproximate)
            return channelResult;
            
        Result<ApproximateChannelParsingResults> approxChannelResult;
        if ((approxChannelResult = TryParseChannelFromNameLevenshtein(value, guildChannels)).IsSuccess)
            return approxChannelResult.Entity;
            
        return channelResult;
    }

    /// <inheritdoc />
    public override ValueTask<Result<IChannel>> TryParseAsync(string value, CancellationToken ct = default)
    {
        var result = TryParse(value, false);
        if (!result.IsSuccess)
            return ValueTask.FromResult(Result<IChannel>.FromError(result.Error));

        return ValueTask.FromResult(Result<IChannel>.FromSuccess(result.Entity.Channels[0]));
    }
}
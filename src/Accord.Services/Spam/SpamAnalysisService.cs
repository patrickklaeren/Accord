using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Accord.Services.Spam;

[RegisterSingleton]
internal sealed class SpamAnalysisService
{
    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<SpamMessage>> _userMessages = new();

    public IReadOnlyCollection<SpamMatch> TryGetSpamMatches(ulong userId,
        ulong messageId,
        ulong channelId,
        string content,
        int spamMessageThreshold,
        int spamMessageWindowInSeconds)
    {
        var spamWindow = TimeSpan.FromSeconds(spamMessageWindowInSeconds);
        var now = DateTimeOffset.UtcNow;
        var messages = _userMessages.GetOrAdd(userId, _ => new ConcurrentQueue<SpamMessage>());

        messages.Enqueue(new SpamMessage(messageId, channelId, content, now));

        while (messages.TryPeek(out var oldest) && now - oldest.Timestamp > spamWindow)
        {
            messages.TryDequeue(out _);
        }

        var duplicates = messages
            .Where(m => string.Equals(m.Content, content, StringComparison.OrdinalIgnoreCase) && m.MessageId != messageId)
            .ToList();

        return duplicates.Count <= spamMessageThreshold
            ? []
            : duplicates.Select(m => new SpamMatch(m.MessageId, m.ChannelId)).ToList();
    }

    private sealed record SpamMessage(ulong MessageId, ulong ChannelId, string Content, DateTimeOffset Timestamp);
}

public sealed record SpamMatch(ulong DiscordMessageId, ulong DiscordChannelId);

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Accord.Bot.Helpers;

[RegisterSingleton]
public class VoiceChannelLeaseOccupancyTracker
{
    private sealed class ChannelState
    {
        public ConcurrentDictionary<ulong, byte> Occupants { get; } = new(); // there is no such thing as ConcurrentHashSet so as a workaround I just did a Dictionary and we ignore the value ~ Theos
        public volatile bool HasEverBeenOccupied;
    }

    private readonly ConcurrentDictionary<ulong, ChannelState> _channels = new();

    public bool HasSeeded { get; private set; }

    public void StartTracking(ulong channelId)
    {
        _channels.TryAdd(channelId, new ChannelState());
    }

    public void StopTracking(ulong channelId)
    {
        _channels.TryRemove(channelId, out _);
    }

    public void ApplyVoiceState(ulong userId, ulong? channelId)
    {
        foreach (var (trackedChannelId, state) in _channels)
        {
            if (channelId != trackedChannelId)
            {
                state.Occupants.TryRemove(userId, out _);
            }
        }

        if (channelId is not (null or 0)
            && _channels.TryGetValue(channelId.Value, out var target))
        {
            target.Occupants[userId] = 0;
            target.HasEverBeenOccupied = true;
        }
    }

    public void Seed(IEnumerable<ulong> leasedChannelIds, IEnumerable<(ulong UserId, ulong? ChannelId)> voiceStates)
    {
        _channels.Clear();

        foreach (var channelId in leasedChannelIds)
        {
            _channels.TryAdd(channelId, new ChannelState());
        }

        foreach (var (userId, channelId) in voiceStates)
        {
            if (channelId is not (null or 0)
                && _channels.TryGetValue(channelId.Value, out var state))
            {
                state.Occupants[userId] = 0;
                state.HasEverBeenOccupied = true;
            }
        }

        HasSeeded = true;
    }

    public bool IsChannelEmpty(ulong channelId)
    {
        return _channels.TryGetValue(channelId, out var state) && state.Occupants.IsEmpty;
    }

    public bool HasEverBeenOccupied(ulong channelId)
    {
        return _channels.TryGetValue(channelId, out var state) && state.HasEverBeenOccupied;
    }
}

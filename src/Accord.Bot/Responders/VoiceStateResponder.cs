using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.VoiceSessions;
using MediatR;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class VoiceStateResponder : IResponder<IVoiceStateUpdate>
{
    private readonly IEventQueue _eventQueue;

    public VoiceStateResponder(IEventQueue eventQueue)
    {
        _eventQueue = eventQueue;
    }

    public async Task<Result> RespondAsync(IVoiceStateUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (!gatewayEvent.Member.HasValue
            || !gatewayEvent.Member.Value.User.HasValue)
            return Result.FromSuccess();

        if (gatewayEvent.Member.Value.User.Value.IsBot == true)
            return Result.FromSuccess();

        IRequest type = gatewayEvent.ChannelID.HasValue
            ? new StartVoiceSessionRequest(gatewayEvent.GuildID.Value.Value, 
                gatewayEvent.UserID.Value,
                gatewayEvent.ChannelID.Value.Value,
                gatewayEvent.SessionID,
                DateTimeOffset.Now)
            : new FinishVoiceSessionRequest(gatewayEvent.GuildID.Value.Value, 
                gatewayEvent.UserID.Value,
                gatewayEvent.SessionID,
                DateTimeOffset.Now);

        await _eventQueue.Queue(type);

        return Result.FromSuccess();
    }
}
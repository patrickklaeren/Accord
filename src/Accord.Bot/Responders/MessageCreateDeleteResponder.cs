using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services;
using Accord.Services.UserMessages;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class MessageCreateDeleteResponder : IResponder<IMessageCreate>, 
    IResponder<IMessageDelete>, 
    IResponder<IMessageDeleteBulk>
{
    private readonly IEventQueue _eventQueue;

    public MessageCreateDeleteResponder(IEventQueue eventQueue)
    {
        _eventQueue = eventQueue;
    }

    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
            return Result.FromSuccess();

        await _eventQueue.Queue(new AddMessageRequest(gatewayEvent.ID.Value,
            gatewayEvent.Author.ID.Value, gatewayEvent.ChannelID.Value,
            gatewayEvent.Timestamp));

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        await _eventQueue.Queue(new DeleteMessageRequest(gatewayEvent.ID.Value));
        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageDeleteBulk gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        foreach (var id in gatewayEvent.IDs)
        {
            await _eventQueue.Queue(new DeleteMessageRequest(id.Value));
        }

        return Result.FromSuccess();
    }
}
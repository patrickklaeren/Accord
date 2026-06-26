using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services;
using Accord.Services.Spam;
using Accord.Services.UserMessages;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Accord.Bot.Responders;

public class MessageCreateDeleteResponder(CoreEventQueue eventQueue,
    SpamEventQueue spamEventQueue,
    PermissionUserFactory permissionUserFactory) 
    : IResponder<IMessageCreate>,
    IResponder<IMessageDelete>,
    IResponder<IMessageDeleteBulk>
{
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Author.IsBot.HasValue || gatewayEvent.Author.IsSystem.HasValue)
            return Result.FromSuccess();

        var fileUrls = gatewayEvent.Attachments
            .Where(x => x.ContentType.HasValue && x.ContentType.Value.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Url)
            .ToList();

        if (fileUrls.Any())
        {
            await eventQueue.Queue(new IsCryptoSpamMessageRequest(gatewayEvent.GuildID.Value.Value, gatewayEvent.Author.ID.Value, fileUrls));
        }

        await eventQueue.Queue(new AddMessageRequest(gatewayEvent.ID.Value,
            gatewayEvent.Author.ID.Value, 
            gatewayEvent.ChannelID.Value,
            gatewayEvent.Timestamp));
        
        var permissionUser = await permissionUserFactory.FromId(gatewayEvent.Author.ID.Value);

        await spamEventQueue.Queue(new AddSpamCheckMessageRequest(
            gatewayEvent.ID.Value,
            gatewayEvent.ChannelID.Value,
            gatewayEvent.Content,
            permissionUser));

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        await eventQueue.Queue(new DeleteMessageRequest(gatewayEvent.ID.Value));
        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageDeleteBulk gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        foreach (var id in gatewayEvent.IDs)
        {
            await eventQueue.Queue(new DeleteMessageRequest(id.Value));
        }

        return Result.FromSuccess();
    }
}
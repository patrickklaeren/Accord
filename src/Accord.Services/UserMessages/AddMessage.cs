using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.UserMessages;

public sealed record AddMessageRequest(ulong DiscordMessageId, ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset SentDateTime)
    : IRequest, IEnsureUserExistsRequest;

public class AddMessageHandler(AccordContext db) : IRequestHandler<AddMessageRequest>
{

    public async Task Handle(AddMessageRequest request, CancellationToken cancellationToken)
    {
        var message = new UserMessage
        {
            Id = request.DiscordMessageId,
            UserId = request.DiscordUserId,
            DiscordChannelId = request.DiscordChannelId,
            SentDateTime = request.SentDateTime,
        };

        db.Add(message);

        await db.SaveChangesAsync(cancellationToken);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.UserMessages;

public sealed record AddMessageRequest(ulong DiscordMessageId, 
    ulong DiscordUserId, 
    ulong DiscordChannelId, 
    string? Content,
    IReadOnlyCollection<string> AttachmentsDetail,
    DateTimeOffset SentDateTime)
    : IRequest, IEnsureUserExistsRequest;

internal class AddMessageHandler(AccordContext db) : IRequestHandler<AddMessageRequest>
{
    public async Task Handle(AddMessageRequest request, CancellationToken cancellationToken)
    {
        var message = new UserMessage
        {
            Id = request.DiscordMessageId,
            UserId = request.DiscordUserId,
            DiscordChannelId = request.DiscordChannelId,
            Content = request.Content,
            AttachmentsDetail = request.AttachmentsDetail.Any() 
                ? string.Join(", ", request.AttachmentsDetail) 
                : null,
            SentDateTime = request.SentDateTime,
        };

        db.UserMessages.Add(message);

        await db.SaveChangesAsync(cancellationToken);
    }
}
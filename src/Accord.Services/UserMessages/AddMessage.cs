using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.UserMessages;

public sealed record AddMessageRequest(ulong DiscordMessageId, ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset SentDateTime) 
    : IRequest, IEnsureUserExistsRequest;

public class AddMessageHandler : AsyncRequestHandler<AddMessageRequest>
{
    private readonly AccordContext _db;

    public AddMessageHandler(AccordContext db)
    {
        _db = db;
    }

    protected override async Task Handle(AddMessageRequest request, CancellationToken cancellationToken)
    {
        var message = new UserMessage
        {
            Id = request.DiscordMessageId,
            UserId = request.DiscordUserId,
            DiscordChannelId = request.DiscordChannelId,
            SentDateTime = request.SentDateTime,
        };

        _db.Add(message);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
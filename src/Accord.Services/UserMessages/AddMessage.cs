﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.UserMessages;

public sealed record AddMessageRequest(ulong DiscordMessageId, ulong DiscordUserId, ulong DiscordChannelId, DateTimeOffset SentDateTime) 
    : IRequest, IEnsureUserExistsRequest;

[AutoConstructor]
public partial class AddMessageHandler : IRequestHandler<AddMessageRequest>
{
    private readonly AccordContext _db;

    public async Task Handle(AddMessageRequest request, CancellationToken cancellationToken)
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
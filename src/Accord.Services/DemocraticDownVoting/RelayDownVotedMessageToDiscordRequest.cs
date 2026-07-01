using System.Collections.Generic;
using MediatR;

namespace Accord.Services.DemocraticDownVoting;

public sealed record RelayDownVotedMessageToDiscordRequest(ulong DownVotedMessageId, 
    ulong DownVotedMessageChannelId, 
    ulong DownVotedMessageAuthorId,
    IReadOnlyCollection<ulong> DownVotedByUserIds) : IRequest;
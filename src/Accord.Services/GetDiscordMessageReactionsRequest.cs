using System.Collections.Generic;
using MediatR;

namespace Accord.Services;

public sealed record GetDiscordMessageReactionsRequest(ulong DiscordChannelId, ulong DiscordMessageId, string Emoji) 
    : IRequest<ReactedDiscordMessageDto?>;

public sealed record ReactedDiscordMessageDto(ulong Id, ulong AuthorId, IReadOnlyCollection<ulong> ReactedByUserIds);

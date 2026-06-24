using System.Collections.Generic;
using MediatR;

namespace Accord.Services.Starboard;

public sealed record GetDiscordMessageReactionsRequest(ulong DiscordChannelId, ulong DiscordMessageId) 
    : IRequest<IReadOnlyCollection<MessageReactionDto>>;

public sealed record MessageReactionDto(string EmojiName, int Count);

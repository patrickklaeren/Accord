using System.Collections.Generic;
using MediatR;

namespace Accord.Services.Starboard;

public sealed record GetStarredDiscordMessageRequest(ulong DiscordChannelId, ulong DiscordMessageId) 
    : IRequest<StarredDiscordMessageDto?>;

public sealed record StarredDiscordMessageDto(ulong Id, ulong AuthorId, IReadOnlyCollection<ulong> StarredByUserIds);

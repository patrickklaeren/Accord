using MediatR;

namespace Accord.Services.Starboard;

public sealed record RelayNewStarboardEntryToDiscordRequest(ulong PostToDiscordChannelId, ulong StarredDiscordMessageId, ulong StarredDiscordMessageChannelId, string StarEmoji, int NumberOfStars) : IRequest<ulong?>;
public sealed record RelayExistingStarboardEntryToDiscordRequest(ulong DiscordMessageIdToEdit, ulong DiscordMessageChannelIdToEdit, ulong StarredDiscordMessageId, ulong StarredDiscordMessageChannelId, string StarEmoji, int NumberOfStars) : IRequest;
public sealed record DeleteStarboardEntryToDiscordRequest(ulong DiscordMessageIdToEdit, ulong DiscordMessageChannelIdToEdit) : IRequest;
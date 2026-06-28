using MediatR;

namespace Accord.Services.UserMessages;

public sealed record RelayMessageUpdateToDiscord(ulong DiscordMessageId, ulong DiscordChannelId, ulong DiscordUserId, string? OldContent, string? NewContent) : INotification;
public sealed record RelayMessageDeleteToDiscord(ulong DiscordMessageId, ulong DiscordChannelId, ulong DiscordUserId, string? Content, string? AttachmentsDetail) : INotification;
using MediatR;

namespace Accord.Services;

public sealed record RelayKickToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
public sealed record RelayBanToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
public sealed record RelayUnbanToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
public sealed record RelayWarningToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
public sealed record RelayMuteToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
public sealed record RelayUnmuteToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
public sealed record RelayNoteToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : INotification;
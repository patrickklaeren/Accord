using MediatR;

namespace Accord.Services;

public sealed record RelayKickToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : IRequest;
public sealed record RelayBanToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : IRequest;
public sealed record RelayUnbanToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : IRequest;
public sealed record RelayWarningToDiscordRequest(ulong ActingDiscordUserId, ulong TargetDiscordUserId, string? Reason) : IRequest;
using MediatR;

namespace Accord.Services.Users;

public sealed record VoiceUnmuteUserInDiscordRequest(ulong DiscordUserId) : INotification;

using System.Collections.Generic;
using MediatR;

namespace Accord.Services.Spam;

public sealed record CleanUpSpamRequest(
    ulong DiscordUserId,
    IReadOnlyCollection<SpamMatch> Messages,
    int TimeoutInSeconds
) : INotification;

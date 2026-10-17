using System;
using Accord.Services.Users;

namespace Accord.Web.Models;

public sealed record DashboardViewModel
{
    public ulong UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public string? Nickname { get; init; }
    public bool IsGuildMember { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public GetUserProfileDto? Profile { get; init; }
    public string? MostActiveTextChannel { get; init; }
    public string? MostActiveVoiceChannel { get; init; }
    public string? Error { get; init; }
}

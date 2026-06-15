using System;

namespace Accord.Services.Moderation;

public sealed record GuildUserDto(ulong Id, string? Username, string? DiscordAvatarUrl, DateTimeOffset JoinedDateTime);
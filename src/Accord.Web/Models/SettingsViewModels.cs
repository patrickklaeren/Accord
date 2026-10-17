using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Guilds;
using Accord.Services.Permissions;

namespace Accord.Web.Models;

public sealed record PermissionsViewModel(
    IReadOnlyList<GuildRoleDto> Roles,
    GetAllPermissionsResponse Permissions,
    IReadOnlyDictionary<ulong, string> UserNames)
{
    public PermissionType[] Types { get; } = Enum.GetValues<PermissionType>();
}

public sealed record ChannelFlagsViewModel(
    IReadOnlyList<GuildChannelDto> Channels,
    IReadOnlyCollection<ChannelFlagDto> Flags)
{
    public ChannelFlagType[] Types { get; } = Enum.GetValues<ChannelFlagType>();
}

public sealed record PermissionInputModel
{
    [Range(typeof(ulong), "1", "18446744073709551615", ErrorMessage = "Enter a valid Discord ID.")]
    public ulong SubjectId { get; init; }

    [EnumDataType(typeof(PermissionType))]
    public PermissionType Permission { get; init; }

    public bool Enabled { get; init; }
}

public sealed record ChannelFlagInputModel
{
    [Range(typeof(ulong), "1", "18446744073709551615", ErrorMessage = "Enter a valid Discord channel ID.")]
    public ulong ChannelId { get; init; }

    [EnumDataType(typeof(ChannelFlagType))]
    public ChannelFlagType Flag { get; init; }

    public bool Enabled { get; init; }
}

public static class DisplayNames
{
    public static string For(Enum value) => value switch
    {
        ChannelFlagType.IgnoredFromXp => "Ignored from XP",
        ChannelFlagType.JoinLeaveLogs => "Join/leave logs",
        PermissionType.CreateShortUrls => "Create short URLs",
        _ => Regex.Replace(value.ToString(), "(?<!^)([A-Z])", " $1")
    };
}

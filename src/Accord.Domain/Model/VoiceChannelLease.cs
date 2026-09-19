using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model;

public class VoiceChannelLease
{
    public int Id { get; set; }

    public ulong OwnerUserId { get; set; }
    [InverseProperty(nameof(User.VoiceChannelLeasesOwned))]
    public User? OwnerUser { get; set; }

    public ulong DiscordGuildId { get; set; }
    public ulong DiscordCategoryId { get; set; }
    public ulong DiscordChannelId { get; set; }
    public string ChannelName { get; set; } = null!;
    public int? MaxUsers { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset? ClosedDateTime { get; set; }

    public ulong? ClosedByUserId { get; set; }
    [InverseProperty(nameof(User.VoiceChannelLeasesClosedByUser))]
    public User? ClosedByUser { get; set; }
}
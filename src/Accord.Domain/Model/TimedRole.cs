using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class TimedRole
{
    public ulong DiscordUserId { get; set; }
    public ulong DiscordRoleId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public class TimedRoleEntityTypeConfiguration : IEntityTypeConfiguration<TimedRole>
{
    public void Configure(EntityTypeBuilder<TimedRole> builder)
    {
        builder.HasKey(x => new { x.DiscordUserId, x.DiscordRoleId });
        builder.HasIndex(x => x.ExpiresAt);
    }
}

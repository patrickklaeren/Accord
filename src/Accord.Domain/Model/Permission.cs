using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public abstract class Permission
{
    public int Id { get; set; }
    public PermissionType Type { get; set; }
}

public class UserPermission : Permission
{
    public ulong UserId { get; set; }
    public User? User { get; set; }
}

public class RolePermission : Permission
{
    public ulong RoleId { get; set; }
}

public enum PermissionType
{
    ManageFlags = 0,
    ManageHistories = 4,
    AddHistories = 5,
    ManageTags = 6,
    CreateShortUrls = 7,
    TemporaryMute = 8,
    BypassSpamCheck = 9,
    ForumHelper = 10,
    BypassDownVotes = 11,
    RoleCanBeCampaignedFor = 12,
    CanLeaseVoiceChannels = 13
}

public class UserPermissionEntityTypeConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder
            .HasIndex(x => new { x.UserId, x.Type })
            .IsUnique();
    }
}

public class RolePermissionEntityTypeConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder
            .HasIndex(x => new { x.RoleId, x.Type })
            .IsUnique();
    }
}
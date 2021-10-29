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
    public virtual User User { get; set; } = null!;
}

public class RolePermission : Permission
{
    public ulong RoleId { get; set; }
}

public enum PermissionType
{
    ManageFlags = 0,
    ManageUserReports = 1,
    ManagePatterns = 2,
    ListRiskyUsers = 3,
    BypassRaidCheck = 4
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
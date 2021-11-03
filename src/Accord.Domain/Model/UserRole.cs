using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class UserRole
{
    public UserRole()
    {
        
    }
    
    public UserRole(ulong discordUserId, ulong discordRoleId)
    {
        DiscordUserId = discordUserId;
        DiscordRoleId = discordRoleId;
    }

    public ulong DiscordUserId { get; set; }
    public ulong DiscordRoleId { get; set; }
}

public class UserRoleEntityTypeConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(x => new { x.DiscordUserId, x.DiscordRoleId });
    }
}
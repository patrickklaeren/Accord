using Accord.Domain.Model;
using Accord.Domain.Model.UserReports;
using Microsoft.EntityFrameworkCore;

namespace Accord.Domain;

public class AccordContext : DbContext
{
    public AccordContext(DbContextOptions<AccordContext> builderOptions) : base(builderOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccordContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ChannelFlag> ChannelFlags { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserPermission> UserPermissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<VoiceSession> VoiceConnections { get; set; } = null!;
    public DbSet<RunOption> RunOptions { get; set; } = null!;
    public DbSet<UserMessage> UserMessages { get; set; } = null!;
    public DbSet<NamePattern> NamePatterns { get; set; } = null!;
    public DbSet<UserReminder> UserReminders { get; set; } = null!;
    public DbSet<UserHiddenChannel> UserHiddenChannels { get; set; } = null!;
    public DbSet<UserReport> UserReports { get; set; } = null!;
    public DbSet<UserReportMessage> UserReportMessages { get; set; } = null!;
    public DbSet<UserReportBlock> UserReportBlocks { get; set; } = null!;
}
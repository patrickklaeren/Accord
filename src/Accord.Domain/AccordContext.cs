using Accord.Domain.Model;
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
    public DbSet<UserHistory> UserHistories { get; set; } = null!;
    public DbSet<UserMessage> UserMessages { get; set; } = null!;
    public DbSet<UserReminder> UserReminders { get; set; } = null!;
    public DbSet<UserBotMessage> UserBotMessages { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<TagAlias> TagAliases { get; set; } = null!;
    public DbSet<StarboardChannel> StarboardChannels { get; set; } = null!;
    public DbSet<StarboardEntry> StarboardEntries { get; set; } = null!;
    public DbSet<StarboardEntryOutput> StarboardEntryOutputs { get; set; } = null!;
    public DbSet<RssFeed> RssFeeds { get; set; } = null!;
    public DbSet<RssFeedPost> RssFeedPosts { get; set; } = null!;
    public DbSet<PromotionCampaign> PromotionCampaigns { get; set; } = null!;
    public DbSet<PromotionCampaignOutput> PromotionCampaignOutputs { get; set; } = null!;
    public DbSet<PromotionCampaignVote> PromotionCampaignVotes { get; set; } = null!;
    public DbSet<VoiceChannelLease> VoiceChannelLeases { get; set; } = null!;
}

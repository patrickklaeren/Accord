using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class ChannelFlag
{
    public int Id { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ChannelFlagType Type { get; set; }
}

public enum ChannelFlagType
{
    IgnoredFromXp = 0,
    JoinLeaveLogs = 1,
    UserUpdateLogs = 2,
    UserHistoryLogs = 3,
    RaidLogs = 4,
    VoiceLogs = 6,
    BotLogs = 7,
    IgnoreStarredMessages = 10,
    HelpForum = 11,
    MessageLogs = 12,
    DoNotLogMessages = 13,
    PromotionCampaigns = 14,
    VoiceLeaseCategory = 15
}

public class ChannelFlagEntityTypeConfiguration : IEntityTypeConfiguration<ChannelFlag>
{
    public void Configure(EntityTypeBuilder<ChannelFlag> builder)
    {
        builder
            .HasIndex(x => new {x.DiscordChannelId, x.Type})
            .IsUnique();
    }
}
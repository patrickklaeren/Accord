using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model
{
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
        NameChangeLogs = 2,
        BanKickLogs = 3,
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
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class StarboardEntryOutput
{
    public int StarboardEntryId { get; set; }
    public StarboardEntry? StarboardEntry { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong DiscordMessageId { get; set; }
}

public class StarboardEntryOutputEntityTypeConfiguration : IEntityTypeConfiguration<StarboardEntryOutput>
{
    public void Configure(EntityTypeBuilder<StarboardEntryOutput> builder)
    {
        builder.HasKey(x => new { x.StarboardEntryId, x.DiscordMessageId, x.DiscordChannelId });
    }
}
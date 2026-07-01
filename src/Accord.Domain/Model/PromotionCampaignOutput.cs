using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class PromotionCampaignOutput
{
    public int PromotionCampaignId { get; set; }
    public PromotionCampaign? PromotionCampaign { get; set; }
    public ulong DiscordChannelId { get; set; }
    public ulong DiscordMessageId { get; set; }
}

public class PromotionCampaignOutputEntityTypeConfiguration : IEntityTypeConfiguration<PromotionCampaignOutput>
{
    public void Configure(EntityTypeBuilder<PromotionCampaignOutput> builder)
    {
        builder.HasKey(x => new { x.PromotionCampaignId, x.DiscordMessageId, x.DiscordChannelId });
    }
}
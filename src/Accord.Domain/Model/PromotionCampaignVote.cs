using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accord.Domain.Model;

public class PromotionCampaignVote
{
    public int PromotionCampaignId { get; set; }
    public PromotionCampaign? PromotionCampaign { get; set; }

    public ulong VotingUserId { get; set; }
    public User? VotingUser { get; set; }

    public int Vote { get; set; }
    public DateTimeOffset AtDateTime { get; set; }
}

public class PromotionCampaignVoteEntityTypeConfiguration : IEntityTypeConfiguration<PromotionCampaignVote>
{
    public void Configure(EntityTypeBuilder<PromotionCampaignVote> builder)
    {
        builder.HasKey(x => new { x.PromotionCampaignId, x.VotingUserId });
    }
}
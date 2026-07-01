using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accord.Domain.Model;

public class PromotionCampaign
{
    public int Id { get; set; }

    public ulong ForUserId { get; set; }
    [InverseProperty(nameof(User.PromotionCampaignsForUser))]
    public User? ForUser { get; set; }

    public ulong ByUserId { get; set; }
    [InverseProperty(nameof(User.PromotionCampaignsByUser))]
    public User? ByUser { get; set; }

    public ulong VouchedForByUserId { get; set; }
    [InverseProperty(nameof(User.PromotionCampaignsVouchedForByUser))]
    public User? VouchedByUser { get; set; }
    
    public ulong? ToDiscordRoleId { get; set; }
    
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public DateTimeOffset? ClosedDateTime { get; set; }
    
    public int VoteThresholdRequired { get; set; }
    
    public ulong? ClosedByUserId { get; set; }
    [InverseProperty(nameof(User.PromotionCampaignsClosedByUser))]
    public User? ClosedByUser { get; set; }

    public ICollection<PromotionCampaignVote> Votes { get; set; } = new HashSet<PromotionCampaignVote>();
}
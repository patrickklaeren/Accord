using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Permissions;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.PromotionCampaigns;

[RegisterScoped]
public class PromotionCampaignService(AccordContext db, 
    UserService userService, 
    UserPermissionService userPermissionService,
    ChannelFlagService channelFlagService,
    IMediator mediator)
{
    public async Task<ServiceResponse<PromotionCampaignDto>> AddCampaign(
        PermissionUser forUser,
        PermissionUser byUser,
        PermissionUser vouchedForByUser,
        ulong toDiscordRoleId,
        CancellationToken cancellationToken)
    {
        if (forUser.OwnedDiscordRoleIds.Contains(toDiscordRoleId))
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Campaigning user already has role");
        }

        if (!vouchedForByUser.OwnedDiscordRoleIds.Contains(toDiscordRoleId) && !vouchedForByUser.IsAdministrator)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Vouching user must have role being campaigned for");
        }
        
        if (!await userPermissionService.RoleHasPermission(toDiscordRoleId, PermissionType.RoleCanBeCampaignedFor, cancellationToken))
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Role cannot be campaigned for");
        }

        foreach (var userId in new[] { forUser.DiscordUserId, byUser.DiscordUserId, vouchedForByUser.DiscordUserId }.Distinct())
        {
            await userService.EnsureUserExists(userId, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var threshold = CalculateThreshold();

        var campaign = new PromotionCampaign
        {
            ForUserId = forUser.DiscordUserId,
            ByUserId = byUser.DiscordUserId,
            VouchedForByUserId = vouchedForByUser.DiscordUserId,
            ToDiscordRoleId = toDiscordRoleId,
            StartDateTime = now,
            EndDateTime = now.AddDays(3),
            VoteThresholdRequired = threshold,
        };

        db.PromotionCampaigns.Add(campaign);
        await db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(campaign);
        var channelIds = await channelFlagService.GetChannelIdsWithFlag(ChannelFlagType.PromotionCampaigns, cancellationToken);

        foreach (var channel in channelIds)
        {
            var response = await mediator.Send(new RelayNewPromotionCampaignToDiscordRequest(channel, dto), cancellationToken);

            if (response is null)
                continue;

            var output = new PromotionCampaignOutput
            {
                PromotionCampaignId = campaign.Id,
                DiscordChannelId = channel,
                DiscordMessageId = response.Value
            };

            db.PromotionCampaignOutputs.Add(output);
        }
        
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResponse.Ok(dto);
    }

    public async Task<ServiceResponse<PromotionCampaignDto>> AddPositiveVote(
        int promotionCampaignId,
        ulong votingUserId,
        CancellationToken cancellationToken)
    {
        var campaign = await db.PromotionCampaigns
            .Include(x => x.Outputs)
            .Include(x => x.Votes)
            .SingleAsync(x => x.Id == promotionCampaignId, cancellationToken);

        if (campaign.ForUserId == votingUserId)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("You cannot vote on your own promotion campaign");
        }

        if (campaign.EndDateTime <= DateTimeOffset.UtcNow 
            || campaign.ClosedDateTime is not null)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Campaign is not accepting votes");
        }

        await userService.EnsureUserExists(votingUserId, cancellationToken);

        var existingVote = campaign.Votes.SingleOrDefault(x => x.VotingUserId == votingUserId);
        
        if (existingVote is null)
        {
            var vote = new PromotionCampaignVote
            {
                PromotionCampaignId = promotionCampaignId,
                VotingUserId = votingUserId,
                Vote = 1,
                AtDateTime = DateTimeOffset.UtcNow,
            };
            
            campaign.Votes.Add(vote);
            db.PromotionCampaignVotes.Add(vote);
            await db.SaveChangesAsync(cancellationToken);
        }

        var dto = ToDto(campaign);
        
        foreach (var output in campaign.Outputs)
        {
            await mediator.Send(new RelayExistingPromotionCampaignToDiscordRequest(output.DiscordChannelId, output.DiscordMessageId, dto), cancellationToken);
        }

        return ServiceResponse.Ok(dto);
    }

    public async Task<PromotionCampaignDto?> GetCampaign(int promotionCampaignId, CancellationToken cancellationToken)
    {
        return await db.PromotionCampaigns
            .Where(x => x.Id == promotionCampaignId)
            .Select(x => new PromotionCampaignDto(
                x.Id,
                x.ForUserId,
                x.ByUserId,
                x.VouchedForByUserId,
                x.ToDiscordRoleId!.Value,
                x.VoteThresholdRequired,
                x.Votes.Sum(v => v.Vote),
                x.StartDateTime,
                x.EndDateTime))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static int CalculateThreshold() => 1;

    private static PromotionCampaignDto ToDto(PromotionCampaign campaign)
    {
        return new PromotionCampaignDto(
            campaign.Id,
            campaign.ForUserId,
            campaign.ByUserId,
            campaign.VouchedForByUserId,
            campaign.ToDiscordRoleId!.Value,
            campaign.VoteThresholdRequired,
            campaign.Votes.Sum(v => v.Vote),
            campaign.StartDateTime,
            campaign.EndDateTime);
    }
}

public sealed record PromotionCampaignDto(
    int Id,
    ulong ForUserId,
    ulong ByUserId,
    ulong VouchedForByUserId,
    ulong ToDiscordRoleId,
    int VoteThresholdRequired,
    int VoteProgress,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime);

using System;
using System.Collections.Generic;
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

        var hasExistingCampaign = await db.PromotionCampaigns
            .Where(x => x.ClosedDateTime == null)
            .Where(x => x.ForUserId == forUser.DiscordUserId)
            .AnyAsync(cancellationToken: cancellationToken);

        if (hasExistingCampaign)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Campaigning user has an open campaign, close that one first");
        }

        foreach (var userId in new[] { forUser.DiscordUserId, byUser.DiscordUserId, vouchedForByUser.DiscordUserId }.Distinct())
        {
            await userService.EnsureUserExists(userId, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var threshold = await CalculateThreshold(forUser.DiscordUserId, cancellationToken);

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

    public async Task<ServiceResponse<PromotionCampaignDto>> AddVote(
        int promotionCampaignId,
        ulong votingUserId,
        int vote,
        CancellationToken cancellationToken)
    {
        var campaign = await db.PromotionCampaigns
            .Include(x => x.Outputs)
            .Include(x => x.Votes)
            .SingleAsync(x => x.Id == promotionCampaignId, cancellationToken);

        if (campaign.ForUserId != votingUserId 
            && campaign.EndDateTime > DateTimeOffset.UtcNow 
            && campaign.ClosedDateTime is null)
        {
            await userService.EnsureUserExists(votingUserId, cancellationToken);
            var campaignVote = campaign.Votes.SingleOrDefault(x => x.VotingUserId == votingUserId);

            if (campaignVote is null)
            {
                campaignVote = new PromotionCampaignVote
                {
                    PromotionCampaignId = promotionCampaignId,
                    VotingUserId = votingUserId,
                };
            
                campaign.Votes.Add(campaignVote);
                db.PromotionCampaignVotes.Add(campaignVote);
            }

            campaignVote.Vote = vote;
            campaignVote.AtDateTime = DateTimeOffset.UtcNow;
            
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
                x.ToDiscordRoleId,
                x.VoteThresholdRequired,
                x.Votes.Sum(v => v.Vote),
                x.EndDateTime,
                x.ClosedDateTime,
                x.IsApproved))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActivePromotionCampaignDto>> GetActiveCampaigns(CancellationToken cancellationToken)
    {
        return await db.PromotionCampaigns
            .Where(x => x.ClosedDateTime == null)
            .Where(x => x.EndDateTime > DateTimeOffset.UtcNow)
            .OrderBy(x => x.EndDateTime)
            .Select(x => new ActivePromotionCampaignDto(
                x.Id,
                x.ForUserId,
                x.ToDiscordRoleId,
                x.ByUserId,
                x.VouchedForByUserId,
                x.EndDateTime))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResponse<PromotionCampaignDto>> CloseCampaign(
        int promotionCampaignId,
        ulong closedByUserId,
        CancellationToken cancellationToken)
    {
        var campaign = await db.PromotionCampaigns
            .Include(x => x.Outputs)
            .Include(x => x.Votes)
            .SingleOrDefaultAsync(x => x.Id == promotionCampaignId, cancellationToken);

        if (campaign is null)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Campaign not found");
        }

        return await CloseLoadedCampaign(campaign, closedByUserId, cancellationToken);
    }

    public async Task<ServiceResponse<PromotionCampaignDto>> ApproveCampaign(
        int promotionCampaignId,
        ulong approvedByUserId,
        CancellationToken cancellationToken)
    {
        var campaign = await db.PromotionCampaigns
            .Include(x => x.Outputs)
            .Include(x => x.Votes)
            .SingleOrDefaultAsync(x => x.Id == promotionCampaignId, cancellationToken);

        if (campaign is null)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Campaign not found");
        }

        if (campaign.ClosedDateTime is not null)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Campaign is not open");
        }

        var applyRoleResponse = await mediator.Send(new ApplyPromotionCampaignRoleToDiscordRequest(
            campaign.Id,
            campaign.ForUserId,
            campaign.ToDiscordRoleId,
            approvedByUserId), cancellationToken);

        if (!applyRoleResponse)
        {
            return ServiceResponse.Fail<PromotionCampaignDto>("Failed promoting user");
        }

        campaign.IsApproved = true;

        var response = await CloseLoadedCampaign(campaign, approvedByUserId, cancellationToken);

        if (response.Success)
        {
            foreach (var output in campaign.Outputs)
            {
                await mediator.Send(new RelayApprovedPromotionCampaignToDiscordRequest(output.DiscordChannelId, response.Value!), cancellationToken);
            }
        }

        return response;
    }

    private async Task<ServiceResponse<PromotionCampaignDto>> CloseLoadedCampaign(
        PromotionCampaign campaign,
        ulong closedByUserId,
        CancellationToken cancellationToken)
    {
        campaign.ClosedDateTime ??= DateTimeOffset.UtcNow;
        campaign.ClosedByUserId ??= closedByUserId;

        await db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(campaign);

        foreach (var output in campaign.Outputs)
        {
            await mediator.Send(new RelayExistingPromotionCampaignToDiscordRequest(output.DiscordChannelId,
                output.DiscordMessageId,
                dto), 
                cancellationToken);
        }

        return ServiceResponse.Ok(dto);
    }

    private async Task<int> CalculateThreshold(ulong discordUserId, CancellationToken cancellationToken)
    {
        const int MINIMUM_THRESHOLD = 5;
        const int MAXIMUM_THRESHOLD = 20;
        
        var userStatistics = await db.Users
            .Where(x => x.Id == discordUserId)
            .Select(x => new
            {
                x.FirstSeenDateTime,
                x.JoinedGuildDateTime,
                x.ParticipationRank,
            }).SingleAsync(cancellationToken);

        var dateToUseForThreshold = userStatistics.JoinedGuildDateTime 
                                     ?? userStatistics.FirstSeenDateTime;

        var daysInGuild = Math.Max(0, (DateTimeOffset.UtcNow - dateToUseForThreshold).TotalDays);
        
        var tenureDiscount = daysInGuild switch
        {
            >= 730 => 4,
            >= 365 => 3,
            >= 180 => 2,
            >= 90 => 1,
            _ => 0,
        };

        var participationDiscount = userStatistics.ParticipationRank switch
        {
            <= 0 => 0,
            <= 10 => 4,
            <= 25 => 3,
            <= 50 => 2,
            <= 100 => 1,
            _ => 0,
        };

        return Math.Clamp(MAXIMUM_THRESHOLD - tenureDiscount - participationDiscount,
            MINIMUM_THRESHOLD,
            MAXIMUM_THRESHOLD);
    }

    private static PromotionCampaignDto ToDto(PromotionCampaign campaign)
    {
        return new PromotionCampaignDto(
            campaign.Id,
            campaign.ForUserId,
            campaign.ToDiscordRoleId,
            campaign.VoteThresholdRequired,
            campaign.Votes.Count,
            campaign.EndDateTime,
            campaign.ClosedDateTime,
            campaign.IsApproved);
    }
}

public sealed record PromotionCampaignDto(
    int Id,
    ulong ForUserId,
    ulong ToDiscordRoleId,
    int VoteThresholdRequired,
    int TotalVotes,
    DateTimeOffset EndDateTime,
    DateTimeOffset? ClosedDateTime,
    bool IsApproved);

public sealed record ActivePromotionCampaignDto(
    int Id,
    ulong ForUserId,
    ulong ToDiscordRoleId,
    ulong ByUserId,
    ulong VouchedForByUserId,
    DateTimeOffset EndDateTime);

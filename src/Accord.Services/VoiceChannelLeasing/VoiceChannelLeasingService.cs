using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.VoiceChannelLeasing;

[RegisterScoped]
public class VoiceChannelLeasingService(AccordContext db,
    UserService userService,
    UserPermissionService userPermissionService,
    IMediator mediator)
{
    public const int MIN_USER_LIMIT = 2;
    public const int MAX_USER_LIMIT = 99;

    public async Task<ServiceResponse<VoiceChannelLeaseDto>> LeaseChannel(
        PermissionUser forUser,
        ulong discordGuildId,
        string channelName,
        int? maxUsers,
        CancellationToken cancellationToken)
    {
        if (!await userPermissionService.HasPermission(forUser, PermissionType.CanLeaseVoiceChannels))
        {
            return ServiceResponse.Fail<VoiceChannelLeaseDto>("You do not have permission to lease voice channels");
        }

        if (string.IsNullOrWhiteSpace(channelName))
        {
            return ServiceResponse.Fail<VoiceChannelLeaseDto>("A channel name is required");
        }

        if (maxUsers is not null && (maxUsers < MIN_USER_LIMIT || maxUsers > MAX_USER_LIMIT))
        {
            return ServiceResponse.Fail<VoiceChannelLeaseDto>($"The maximum number of users must be between {MIN_USER_LIMIT} and {MAX_USER_LIMIT}");
        }

        var hasExistingLease = await db.VoiceChannelLeases
            .Where(x => x.ClosedDateTime == null)
            .Where(x => x.OwnerUserId == forUser.DiscordUserId)
            .AnyAsync(cancellationToken);

        if (hasExistingLease)
        {
            return ServiceResponse.Fail<VoiceChannelLeaseDto>("You already have an active leased voice channel");
        }

        var trimmedName = channelName.Trim();

        await userService.EnsureUserExists(forUser.DiscordUserId, cancellationToken);

        var createResponse = await mediator.Send(
            new CreateLeasedVoiceChannelRequest(discordGuildId, trimmedName, maxUsers),
            cancellationToken);

        if (createResponse.Failure)
        {
            return ServiceResponse.Fail<VoiceChannelLeaseDto>(createResponse.ErrorMessage);
        }

        var lease = new VoiceChannelLease
        {
            OwnerUserId = forUser.DiscordUserId,
            DiscordGuildId = discordGuildId,
            DiscordCategoryId = createResponse.Value!.ParentCategoryId ?? 0,
            DiscordChannelId = createResponse.Value.ChannelId,
            ChannelName = trimmedName,
            MaxUsers = maxUsers,
            CreatedDateTime = DateTimeOffset.UtcNow,
        };

        db.VoiceChannelLeases.Add(lease);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok(ToDto(lease));
    }

    public async Task<ServiceResponse<VoiceChannelLeaseDto>> DisbandOwnedChannel(
        PermissionUser forUser,
        CancellationToken cancellationToken)
    {
        var lease = await db.VoiceChannelLeases
            .Where(x => x.ClosedDateTime == null)
            .FirstOrDefaultAsync(x => x.OwnerUserId == forUser.DiscordUserId, cancellationToken);

        if (lease is null)
        {
            return ServiceResponse.Fail<VoiceChannelLeaseDto>("You do not have an active leased voice channel");
        }

        var dto = ToDto(lease);
        await CloseInternal(lease, forUser.DiscordUserId, cancellationToken);

        return ServiceResponse.Ok(dto);
    }

    public async Task<IReadOnlyList<VoiceChannelLeaseDto>> GetActiveLeases(CancellationToken cancellationToken)
    {
        var leases = await db.VoiceChannelLeases
            .Where(x => x.ClosedDateTime == null)
            .ToListAsync(cancellationToken);

        return leases.Select(ToDto).ToList();
    }

    public async Task<ServiceResponse> CloseLease(
        int leaseId,
        ulong? closedByUserId,
        CancellationToken cancellationToken)
    {
        var lease = await db.VoiceChannelLeases
            .Where(x => x.ClosedDateTime == null)
            .FirstOrDefaultAsync(x => x.Id == leaseId, cancellationToken);

        if (lease is null)
        {
            return ServiceResponse.Ok(); // we are returning OK even tho it's null cuz something else might have closed/removed the lease ~ Theos
        }

        await CloseInternal(lease, closedByUserId, cancellationToken);

        return ServiceResponse.Ok();
    }

    private async Task CloseInternal(
        VoiceChannelLease lease,
        ulong? closedByUserId,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteLeasedVoiceChannelRequest(lease.DiscordChannelId), cancellationToken);

        lease.ClosedDateTime = DateTimeOffset.UtcNow;
        lease.ClosedByUserId = closedByUserId;

        await db.SaveChangesAsync(cancellationToken);
    }

    private static VoiceChannelLeaseDto ToDto(VoiceChannelLease lease)
    {
        return new VoiceChannelLeaseDto(
            lease.Id,
            lease.DiscordChannelId,
            lease.ChannelName,
            lease.CreatedDateTime);
    }
}

public sealed record VoiceChannelLeaseDto(
    int Id,
    ulong DiscordChannelId,
    string ChannelName,
    DateTimeOffset CreatedDateTime);

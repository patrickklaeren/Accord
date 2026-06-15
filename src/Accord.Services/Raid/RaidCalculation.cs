using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Moderation;
using Accord.Services.Permissions;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Raid;

public sealed record RaidCalculationRequest(ulong DiscordGuildId, GuildUserDto User) : IRequest, IEnsureUserExistsRequest
{
    public ulong DiscordUserId => User.Id;
}

public sealed record RaidAlertRequest : IRequest;

[AutoConstructor]
public partial class RaidCalculationHandler : IRequestHandler<RaidCalculationRequest>
{
    private readonly RaidCalculator _raidCalculator;
    private readonly IMediator _mediator;
    private readonly AccordContext _db;

    public async Task Handle(RaidCalculationRequest request, CancellationToken cancellationToken)
    {
        var bypassRaidCheck = await _mediator.Send(new UserIsExemptFromRaidRequest(request.User.Id), cancellationToken);

        if (bypassRaidCheck)
        {
            return;
        }

        var sequentialLimit = await _mediator.Send(new GetJoinLimitPerMinuteRequest(), cancellationToken);
        var accountCreationLimit = await _mediator.Send(new GetAccountCreationSimilarityLimitRequest(), cancellationToken);

        var raidResult = _raidCalculator.CalculateIsRaid(new UserJoin(request.User.Id, request.User.DiscordAvatarUrl, request.User.JoinedDateTime.DateTime), sequentialLimit, accountCreationLimit);

        var isAutoRaidModeEnabled = await _mediator.Send(new GetIsAutoRaidModeEnabledRequest(), cancellationToken);
        var isInExistingRaidMode = await _mediator.Send(new GetIsInRaidModeRequest(), cancellationToken);

        if (raidResult.IsRaid != isInExistingRaidMode)
        {
            var runOption = await _db.RunOptions
                .SingleAsync(x => x.Type == RunOptionType.RaidModeEnabled, cancellationToken: cancellationToken);

            runOption.Value = raidResult.IsRaid.ToString();

            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetIsInRaidModeRequest(), cancellationToken);
        }

        if (raidResult.IsRaid && !isInExistingRaidMode)
        {
            // If this is a raid and we haven't already detected a raid prior to this
            // request, send the alert
            await _mediator.Send(new RaidAlertRequest(), cancellationToken);
        }

        if (raidResult.IsRaid && isAutoRaidModeEnabled)
        {
            var user = await _mediator.Send(new GetUserRequest(request.DiscordUserId), cancellationToken);

            if (user.Success)
            {
                var kickRequest = new KickRequest(request.DiscordGuildId,
                    request.DiscordUserId,
                    user.Value!.User.Username ?? request.DiscordUserId.ToString(),
                    $"Auto detection - {raidResult.Reason}");
                
                await _mediator.Send(kickRequest, cancellationToken);
            }
        }
    }
}
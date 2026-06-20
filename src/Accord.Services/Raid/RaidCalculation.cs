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

public class RaidCalculationHandler(
    RaidCalculator raidCalculator,
    IMediator mediator,
    AccordContext db,
    UserService userService)
    : IRequestHandler<RaidCalculationRequest>
{
    public async Task Handle(RaidCalculationRequest request, CancellationToken cancellationToken)
    {
        var sequentialLimit = await mediator.Send(new GetJoinLimitPerMinuteRequest(), cancellationToken);
        var accountCreationLimit = await mediator.Send(new GetAccountCreationSimilarityLimitRequest(), cancellationToken);

        var raidResult = raidCalculator.CalculateIsRaid(new UserJoin(request.User.Id, request.User.DiscordAvatarUrl, request.User.JoinedDateTime.DateTime), sequentialLimit,
            accountCreationLimit);

        var isAutoRaidModeEnabled = await mediator.Send(new GetIsAutoRaidModeEnabledRequest(), cancellationToken);
        var isInExistingRaidMode = await mediator.Send(new GetIsInRaidModeRequest(), cancellationToken);

        if (raidResult.IsRaid != isInExistingRaidMode)
        {
            var runOption = await db.RunOptions
                .SingleAsync(x => x.Type == RunOptionType.RaidModeEnabled, cancellationToken: cancellationToken);

            runOption.Value = raidResult.IsRaid.ToString();

            await db.SaveChangesAsync(cancellationToken);

            await mediator.Send(new InvalidateGetIsInRaidModeRequest(), cancellationToken);
        }

        if (raidResult.IsRaid && !isInExistingRaidMode)
        {
            // If this is a raid and we haven't already detected a raid prior to this
            // request, send the alert
            await mediator.Send(new RaidAlertRequest(), cancellationToken);
        }

        if (raidResult.IsRaid 
            && isAutoRaidModeEnabled 
            && await userService.UserExists(request.DiscordUserId, cancellationToken))
        {
            var user = await userService.GetUser(request.DiscordUserId, cancellationToken);

            var kickRequest = new KickRequest(request.DiscordGuildId,
                request.DiscordUserId,
                user.Username ?? request.DiscordUserId.ToString(),
                $"Auto detection - {raidResult.Reason}");

            await mediator.Send(kickRequest, cancellationToken);
        }
    }
}
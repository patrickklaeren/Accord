using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Moderation;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Raid;

public sealed record RaidCalculationRequest(ulong DiscordGuildId, GuildUserDto User) : IRequest, IEnsureUserExistsRequest
{
    public ulong DiscordUserId => User.Id;
}

public sealed record RaidAlertRequest : IRequest;

internal class RaidCalculationHandler(
    RaidCalculator raidCalculator,
    RunOptionService runOptionService,
    IMediator mediator,
    UserService userService)
    : IRequestHandler<RaidCalculationRequest>
{
    public async Task Handle(RaidCalculationRequest request, CancellationToken cancellationToken)
    {
        var sequentialLimit = await runOptionService.GetOption<int>(RunOptionKey.SequentialJoinsToTriggerRaidMode);
        var accountCreationLimit = await runOptionService.GetOption<int>(RunOptionKey.AccountCreationSimilarityJoinsToTriggerRaidMode);

        var raidResult = raidCalculator
            .CalculateIsRaid(new UserJoin(request.User.Id, request.User.DiscordAvatarUrl, request.User.JoinedDateTime.DateTime),
                sequentialLimit,
                accountCreationLimit);

        var isAutoRaidModeEnabled = await runOptionService.GetOption<bool>(RunOptionKey.AutoRaidModeEnabled);
        var isInExistingRaidMode = await runOptionService.GetOption<bool>(RunOptionKey.IsInRaidMode);

        if (raidResult.IsRaid != isInExistingRaidMode)
        {
            await runOptionService.UpdateOption(RunOptionKey.IsInRaidMode, true);
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
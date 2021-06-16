using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Raid
{
    public sealed record RaidCalculationRequest(DateTimeOffset UserJoinedDateTime) : IRequest<ServiceResponse>;
    public sealed record RaidAlertRequest(bool IsRaidDetected, bool IsInExistingRaidMode, bool IsAutoRaidModeEnabled) : IRequest;

    public class RaidCalculationHandler : IRequestHandler<RaidCalculationRequest, ServiceResponse>
    {
        private readonly RaidCalculator _raidCalculator;
        private readonly IMediator _mediator;
        private readonly AccordContext _db;

        public RaidCalculationHandler(RaidCalculator raidCalculator, IMediator mediator, AccordContext db)
        {
            _raidCalculator = raidCalculator;
            _mediator = mediator;
            _db = db;
        }

        public async Task<ServiceResponse> Handle(RaidCalculationRequest request, CancellationToken cancellationToken)
        {
            var limitPerOneMinute = await _mediator.Send(new GetJoinLimitPerMinuteRequest(), cancellationToken);
            var isRaid = _raidCalculator.CalculateIsRaid(request.UserJoinedDateTime, limitPerOneMinute);
            var isAutoRaidModeEnabled = await _mediator.Send(new GetIsAutoRaidModeEnabledRequest(), cancellationToken);
            var isInExistingRaidMode = await _mediator.Send(new GetIsInRaidModeRequest(), cancellationToken);

            if (isRaid != isInExistingRaidMode)
            {
                var runOption = await _db.RunOptions
                    .SingleAsync(x => x.Type == RunOptionType.RaidModeEnabled, cancellationToken: cancellationToken);

                runOption.Value = isRaid.ToString();

                await _db.SaveChangesAsync(cancellationToken);

                await _mediator.Send(new InvalidateGetIsInRaidModeRequest(), cancellationToken);
            }

            await _mediator.Send(new RaidAlertRequest(isRaid, isInExistingRaidMode, isAutoRaidModeEnabled), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
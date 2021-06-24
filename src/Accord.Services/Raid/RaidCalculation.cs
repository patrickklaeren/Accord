using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Raid
{
    public sealed record RaidCalculationRequest(ulong DiscordGuildId, GuildUserDto User) : IRequest<ServiceResponse>;
    public sealed record GuildUserDto(ulong Id, string Username, string Discriminator, DateTimeOffset JoinedDateTime);
    public sealed record RaidAlertRequest(ulong DiscordGuildId, bool IsRaidDetected, bool IsInExistingRaidMode, bool IsAutoRaidModeEnabled) : IRequest;
    public sealed record KickRequest(ulong DiscordGuildId, GuildUserDto User) : IRequest;

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

            var isRaid = _raidCalculator.CalculateIsRaid(new UserJoin(request.User.Id, request.User.JoinedDateTime.DateTime), limitPerOneMinute);

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

            if (isRaid && !isInExistingRaidMode)
            {
                // If this is a raid and we haven't already detected a raid prior to this
                // request, send the alert
                await _mediator.Send(new RaidAlertRequest(request.DiscordGuildId, isRaid, isInExistingRaidMode, 
                    isAutoRaidModeEnabled), cancellationToken);
            }

            if (isRaid && isAutoRaidModeEnabled)
            {
                await _mediator.Send(new KickRequest(request.DiscordGuildId, 
                    new GuildUserDto(request.User.Id, request.User.Username, request.User.Discriminator, request.User.JoinedDateTime)), 
                    cancellationToken);
            }

            return ServiceResponse.Ok();
        }
    }
}
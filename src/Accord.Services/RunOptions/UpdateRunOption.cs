using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Raid;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.RunOptions;

public sealed record UpdateRunOptionRequest(RunOptionType Type, string RawValue) : IRequest<ServiceResponse>;

public class UpdateRunOptionHandler : IRequestHandler<UpdateRunOptionRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;

    public UpdateRunOptionHandler(AccordContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ServiceResponse> Handle(UpdateRunOptionRequest request, CancellationToken cancellationToken)
    {
        var runOption = await _db.RunOptions
            .SingleAsync(x => x.Type == request.Type, cancellationToken: cancellationToken);

        bool success;

        switch (request.Type)
        {
            case RunOptionType.RaidModeEnabled when bool.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                await _mediator.Send(new InvalidateGetIsInRaidModeRequest(), cancellationToken);
                success = true;
                break;

            case RunOptionType.AutoRaidModeEnabled when bool.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                await _mediator.Send(new InvalidateGetIsAutoRaidModeEnabledRequest(), cancellationToken);
                success = true;
                break;

            case RunOptionType.SequentialJoinsToTriggerRaidMode when int.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                await _mediator.Send(new InvalidateGetJoinLimitPerMinuteRequest(), cancellationToken);
                success = true;
                break;

            case RunOptionType.AccountCreationSimilarityJoinsToTriggerRaidMode when int.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                await _mediator.Send(new InvalidateGetAccountCreationSimilarityLimitRequest(), cancellationToken);
                success = true;
                break;

            case RunOptionType.UserReportsEnabled when bool.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                success = true;
                break;

            case RunOptionType.UserReportsOutboxCategoryId when ulong.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                success = true;
                break;

            case RunOptionType.UserReportsInboxCategoryId when ulong.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                success = true;
                break;

            case RunOptionType.UserReportsAgentRoleId when ulong.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                success = true;
                break;
                
            case RunOptionType.UserHiddenChannelsCascadeHideEnabled when bool.TryParse(request.RawValue, out var actualValue):
                runOption.Value = actualValue.ToString();
                success = true;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request.Type), $"{request.Type} has not been configured to be updated, add the type to {nameof(UpdateRunOptionHandler)}", null);
        }

        if (!success)
        {
            return ServiceResponse.Fail("Failed updating value");
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
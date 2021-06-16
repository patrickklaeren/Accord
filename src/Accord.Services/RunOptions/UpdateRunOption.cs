using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.RunOptions
{
    public sealed record UpdateRunOptionRequest(RunOptionType Type, string RawValue) : IRequest<ServiceResponse>;

    public class UpdateRunOption : IRequestHandler<UpdateRunOptionRequest, ServiceResponse>
    {
        private readonly AccordContext _db;
        private readonly IAppCache _appCache;

        public UpdateRunOption(AccordContext db)
        {
            _db = db;
            _appCache = appCache;
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
                    _appCache.Remove(RaidModeService.BuildGetIsInRaidModeCacheKey());
                    success = true;
                    break;
                case RunOptionType.AutoRaidModeEnabled when bool.TryParse(request.RawValue, out var actualValue):
                    runOption.Value = actualValue.ToString();
                    _appCache.Remove(RaidModeService.BuildGetIsAutoRaidModeEnabledCacheKey());
                    success = true;
                    break;
                case RunOptionType.JoinsToTriggerRaidModePerMinute when int.TryParse(request.RawValue, out var actualValue):
                    runOption.Value = actualValue.ToString();
                    _appCache.Remove(RaidModeService.BuildGetLimitPerOneMinuteCacheKey());
                    success = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Type), request.Type, null);
            }

            if (!success)
            {
                return ServiceResponse.Fail("Failed updating value");
            }

            await _db.SaveChangesAsync(cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Raid;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.RunOptions;

public sealed record UpdateRunOptionRequest(RunOptionKey Key, string RawValue) : IRequest<ServiceResponse>;

internal class UpdateRunOptionHandler(RunOptionService runOptionService) : IRequestHandler<UpdateRunOptionRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(UpdateRunOptionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await runOptionService.UpdateOption(request.Key, request.RawValue);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Fail(ex.Message);
        }
        
        return ServiceResponse.Ok();
    }
}
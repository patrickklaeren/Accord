using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using MediatR;

namespace Accord.Services.ChannelFlags;

public sealed record GetChannelsWithFlagRequest(ChannelFlagType Flag) : IRequest<List<ulong>>;

internal class GetChannelsWithFlagHandler(ChannelFlagService channelFlagService)
    : IRequestHandler<GetChannelsWithFlagRequest, List<ulong>>
{
    public async Task<List<ulong>> Handle(GetChannelsWithFlagRequest request, CancellationToken cancellationToken)
    {
        return await channelFlagService.GetChannelIdsWithFlag(request.Flag, cancellationToken);
    }
}
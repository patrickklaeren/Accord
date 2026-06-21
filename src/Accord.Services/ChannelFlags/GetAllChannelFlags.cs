using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.ChannelFlags;

public sealed record GetAllChannelFlagsRequest : IRequest<GetAllChannelFlagsResponse>;

public sealed record GetAllChannelFlagsResponse(IReadOnlyCollection<ChannelFlagDto> Flags);

public sealed record ChannelFlagDto(ulong DiscordChannelId, ChannelFlagType Type);

public class GetAllChannelFlagsHandler(AccordContext db)
    : IRequestHandler<GetAllChannelFlagsRequest, GetAllChannelFlagsResponse>
{
    public async Task<GetAllChannelFlagsResponse> Handle(GetAllChannelFlagsRequest request, CancellationToken ct)
    {
        var flags = await db.ChannelFlags
            .Select(x => new ChannelFlagDto(x.DiscordChannelId, x.Type))
            .ToListAsync(ct);

        return new GetAllChannelFlagsResponse(flags);
    }
}

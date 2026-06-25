using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Starboard;

public sealed record DeleteStarboardOriginChannelRequest(ulong DiscordChannelBeingStarredInId) : IRequest;

internal class DeleteStarboardOriginChannelHandler(AccordContext db) 
    : IRequestHandler<DeleteStarboardOriginChannelRequest>
{
    public async Task Handle(DeleteStarboardOriginChannelRequest request, CancellationToken cancellationToken)
    {
        await db.StarboardChannels
            .Where(x => x.DiscordChannelBeingStarredInId == request.DiscordChannelBeingStarredInId)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }
}
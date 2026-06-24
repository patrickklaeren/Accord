using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Starboard;

public sealed record DeleteStarboardChannelRequest(ulong DiscordStarboardChannelId) : IRequest;

public class DeleteStarboardChannelHandler(AccordContext db) 
    : IRequestHandler<DeleteStarboardChannelRequest>
{
    public async Task Handle(DeleteStarboardChannelRequest request, CancellationToken cancellationToken)
    {
        await db.StarboardChannels
            .Where(x => x.DiscordStarboardChannelId == request.DiscordStarboardChannelId)
            .ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }
}
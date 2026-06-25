using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Starboard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserMessages;

public sealed record DeleteMessageRequest(ulong DiscordMessageId) : IRequest;

internal class DeleteMessageHandler(AccordContext db, 
    StarboardService starboardService) : IRequestHandler<DeleteMessageRequest>
{
    public async Task Handle(DeleteMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await db.UserMessages
            .SingleOrDefaultAsync(x => x.Id == request.DiscordMessageId, cancellationToken: cancellationToken);

        if (message is not null)
        {
            await starboardService.DeleteStarboardEntriesForMessage(request.DiscordMessageId, cancellationToken);
            db.UserMessages.Remove(message);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
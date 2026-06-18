using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserMessages;

public sealed record DeleteMessageRequest(ulong DiscordMessageId) : IRequest;

public class DeleteMessageHandler(AccordContext db) : IRequestHandler<DeleteMessageRequest>
{

    public async Task Handle(DeleteMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await db.UserMessages.SingleOrDefaultAsync(x => x.Id == request.DiscordMessageId, cancellationToken: cancellationToken);

        if (message is not null)
        {
            db.Remove(message);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
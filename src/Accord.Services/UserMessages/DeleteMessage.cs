using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Starboard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserMessages;

public sealed record DeleteMessageRequest(ulong DiscordMessageId) : IRequest;

internal class DeleteMessageHandler(
    AccordContext db,
    StarboardService starboardService,
    IMediator mediator) : IRequestHandler<DeleteMessageRequest>
{
    public async Task Handle(DeleteMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await db.UserMessages
            .SingleOrDefaultAsync(x => x.Id == request.DiscordMessageId, cancellationToken: cancellationToken);

        if (message is null)
        {
            return;
        }

        await mediator.Publish(new RelayMessageDeleteToDiscord(request.DiscordMessageId,
                message.DiscordChannelId,
                message.UserId,
                message.Content,
                message.AttachmentsDetail),
            cancellationToken);

        db.UserMessages.Remove(message);
        await db.SaveChangesAsync(cancellationToken);

        await starboardService.DeleteStarboardEntriesForMessage(request.DiscordMessageId, cancellationToken);
    }
}
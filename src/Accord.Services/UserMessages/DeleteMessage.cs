using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserMessages;

public sealed record DeleteMessageRequest(ulong DiscordMessageId) : IRequest;

[AutoConstructor]
public partial class DeleteMessageHandler : IRequestHandler<DeleteMessageRequest>
{
    private readonly AccordContext _db;

    public async Task Handle(DeleteMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await _db.UserMessages.SingleOrDefaultAsync(x => x.Id == request.DiscordMessageId, cancellationToken: cancellationToken);

        if (message is not null)
        {
            _db.Remove(message);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
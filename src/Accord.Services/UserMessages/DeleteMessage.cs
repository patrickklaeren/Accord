using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserMessages
{
    public sealed record DeleteMessageRequest(ulong DiscordMessageId) : IRequest;

    public class DeleteMessageHandler : AsyncRequestHandler<DeleteMessageRequest>
    {
        private readonly AccordContext _db;

        public DeleteMessageHandler(AccordContext db)
        {
            _db = db;
        }

        protected override async Task Handle(DeleteMessageRequest request, CancellationToken cancellationToken)
        {
            var message = await _db.UserMessages.SingleOrDefaultAsync(x => x.Id == request.DiscordMessageId, cancellationToken: cancellationToken);

            if (message is null)
                return;

            _db.Remove(message);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

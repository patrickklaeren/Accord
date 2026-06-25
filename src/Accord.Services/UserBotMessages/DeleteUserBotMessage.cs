using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserBotMessages;

public sealed record DeleteUserBotMessageRequest(PermissionUser User, ulong DiscordMessageId) : INotification;

internal class DeleteUserBotMessageHandler(AccordContext db, IMediator mediator) : INotificationHandler<DeleteUserBotMessageRequest>
{
    public async Task Handle(DeleteUserBotMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await db.UserBotMessages
            .Where(x => x.Id == request.DiscordMessageId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (message is null)
            return;

        if (message.UserId != request.User.DiscordUserId && !request.User.IsAdministrator)
        {
            return;
        }

        var response = await mediator.Send(new DeleteDiscordMessageRequest(message.Id, message.DiscordChannelId), cancellationToken);

        if (!response.Success)
            return;

        db.Remove(message);
        await db.SaveChangesAsync(cancellationToken);
    }
}
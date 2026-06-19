using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserBotMessages;

public sealed record DeleteUserBotMessageRequest(PermissionUser User, ulong DiscordMessageId) : IRequest<ServiceResponse>;
public sealed record DeleteDiscordMessageRequest(ulong DiscordMessageId, ulong DiscordChannelId) : IRequest<ServiceResponse>;

public class DeleteUserBotMessage(AccordContext db, IMediator mediator) : IRequestHandler<DeleteUserBotMessageRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(DeleteUserBotMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await db.UserBotMessages
            .Where(x => x.Id == request.DiscordMessageId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (message is null)
        {
            return ServiceResponse.Fail("The message either does not exist or cannot be removed");
        }

        if (message.UserId != request.User.DiscordUserId && !request.User.IsAdministrator)
        {
            return ServiceResponse.Fail("The message can only be removed by the original initiator or administrators");
        }

        await mediator.Send(new DeleteDiscordMessageRequest(message.Id, message.DiscordChannelId), cancellationToken);

        db.Remove(message);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
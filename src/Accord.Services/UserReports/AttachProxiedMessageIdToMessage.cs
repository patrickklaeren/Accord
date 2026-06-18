using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;

namespace Accord.Services.UserReports;

public sealed record AttachProxiedMessageIdToMessageRequest(ulong DiscordMessageId, ulong DiscordProxiedMessageId) : IRequest<ServiceResponse>;

public class AttachProxiedMessageIdToMessageHandler(AccordContext db, IMediator mediator) : IRequestHandler<AttachProxiedMessageIdToMessageRequest, ServiceResponse>
{

    public async Task<ServiceResponse> Handle(AttachProxiedMessageIdToMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await mediator.Send(new GetUserReportMessageRequest(request.DiscordMessageId), cancellationToken);
        if (message is null)
        {
            return ServiceResponse.Fail("Message not found");
        }

        message.DiscordProxyMessageId = request.DiscordProxiedMessageId;
        db.UserReportMessages.Update(message);

        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
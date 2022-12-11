using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;

namespace Accord.Services.UserReports;

public sealed record AttachProxiedMessageIdToMessageRequest(ulong DiscordMessageId, ulong DiscordProxiedMessageId) : IRequest<ServiceResponse>;

[AutoConstructor]
public partial class AttachProxiedMessageIdToMessageHandler : IRequestHandler<AttachProxiedMessageIdToMessageRequest, ServiceResponse>
{
    private readonly AccordContext _db;
    private readonly IMediator _mediator;
    
    public async Task<ServiceResponse> Handle(AttachProxiedMessageIdToMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await _mediator.Send(new GetUserReportMessageRequest(request.DiscordMessageId), cancellationToken);
        if (message is null)
        {
            return ServiceResponse.Fail("Message not found");
        }

        message.DiscordProxyMessageId = request.DiscordProxiedMessageId;
        _db.UserReportMessages.Update(message);
            
        await _db.SaveChangesAsync(cancellationToken);
            
        return ServiceResponse.Ok();
    }
}
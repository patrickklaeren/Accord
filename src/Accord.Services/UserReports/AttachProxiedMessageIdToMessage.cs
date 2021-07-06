using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports
{
    public sealed record AttachProxiedMessageIdToMessageRequest(ulong DiscordMessageId, ulong DiscordProxiedMessageId) : IRequest<ServiceResponse>;
    public class AttachProxiedMessageIdToMessageHandler : IRequestHandler<AttachProxiedMessageIdToMessageRequest, ServiceResponse>
    {
        
        private readonly AccordContext _db;
        private readonly IMediator _mediator;
        
        public AttachProxiedMessageIdToMessageHandler(AccordContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

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
}
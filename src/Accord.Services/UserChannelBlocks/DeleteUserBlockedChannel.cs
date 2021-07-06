using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserChannelBlocks
{
    public class DeleteUserBlockedChannelHandler : IRequestHandler<DeleteUserBlockedChannelRequest, ServiceResponse>
    {
        private readonly IMediator _mediator;
        private readonly AccordContext _accordContext;

        public DeleteUserBlockedChannelHandler(IMediator mediator, AccordContext accordContext)
        {
            _mediator = mediator;
            _accordContext = accordContext;
        }

        public async Task<ServiceResponse> Handle(DeleteUserBlockedChannelRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

            if (!userExists)
                return ServiceResponse.Fail("User does not exist");

            var userBlockedChannels = await _mediator.Send(new GetUserBlockedChannelsRequest(request.DiscordUserId), cancellationToken);

            if (!userBlockedChannels.Contains(request.DiscordChannelId))
            {
                return ServiceResponse.Fail("This channel is not blocked");
            }

            var blockedChannel = await _accordContext.UserBlockedChannels.SingleAsync(x => x.UserId == request.DiscordUserId && x.DiscordChannelId == request.DiscordChannelId,
                cancellationToken: cancellationToken);

            _accordContext.Remove(blockedChannel);

            await _mediator.Send(new InvalidateGetUserBlockedChannelsRequest(request.DiscordUserId), cancellationToken);

            await _accordContext.SaveChangesAsync(cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
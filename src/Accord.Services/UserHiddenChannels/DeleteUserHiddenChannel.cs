using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHiddenChannels
{
    public sealed record DeleteUserHiddenChannelRequest(ulong DiscordUserId, ulong DiscordChannelId) : IRequest<ServiceResponse>;

    public class DeleteUserHiddenChannelHandler : IRequestHandler<DeleteUserHiddenChannelRequest, ServiceResponse>
    {
        private readonly IMediator _mediator;
        private readonly AccordContext _accordContext;

        public DeleteUserHiddenChannelHandler(IMediator mediator, AccordContext accordContext)
        {
            _mediator = mediator;
            _accordContext = accordContext;
        }

        public async Task<ServiceResponse> Handle(DeleteUserHiddenChannelRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

            if (!userExists)
                return ServiceResponse.Fail("User does not exist");

            var userHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

            if (!userHiddenChannels.Contains(request.DiscordChannelId))
            {
                return ServiceResponse.Fail("This channel is not blocked");
            }

            var hiddenChannel = await _accordContext.UserHiddenChannels.SingleAsync(x => x.UserId == request.DiscordUserId && x.DiscordChannelId == request.DiscordChannelId,
                cancellationToken: cancellationToken);

            _accordContext.Remove(hiddenChannel);

            await _mediator.Send(new InvalidateGetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

            await _accordContext.SaveChangesAsync(cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
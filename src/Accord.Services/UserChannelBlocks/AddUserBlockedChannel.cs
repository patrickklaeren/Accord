using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserChannelBlocks
{
    public sealed record AddUserBlockedChannelRequest(ulong DiscordUserId, ulong DiscordChannelId) : IRequest<ServiceResponse>;

    public sealed record DeleteUserBlockedChannelRequest(ulong DiscordUserId, ulong DiscordChannelId) : IRequest<ServiceResponse>;

    public class AddUserBlockedChannelHandler : IRequestHandler<AddUserBlockedChannelRequest, ServiceResponse>
    {
        private readonly AccordContext _accordContext;
        private readonly IMediator _mediator;

        public AddUserBlockedChannelHandler(AccordContext accordContext, IMediator mediator)
        {
            _accordContext = accordContext;
            _mediator = mediator;
        }

        public async Task<ServiceResponse> Handle(AddUserBlockedChannelRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

            if (!userExists)
                return ServiceResponse.Fail("User does not exist");


            var userChannels = await _mediator.Send(new GetUserBlockedChannelsRequest(request.DiscordUserId));

            if (userChannels.Contains(request.DiscordChannelId))
            {
                return ServiceResponse.Fail("This channel is already hidden for you.");
            }

            var userBlockedChannel = new UserBlockedChannel
            {
                UserId = request.DiscordUserId,
                DiscordChannelId = request.DiscordChannelId,
            };

            _accordContext.Add(userBlockedChannel);

            await _accordContext.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetUserBlockedChannelsRequest(request.DiscordUserId), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
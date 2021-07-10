using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserHiddenChannels
{
    public sealed record AddUserHiddenChannelRequest(ulong DiscordUserId, ulong DiscordChannelId) : IRequest<ServiceResponse>;

   public class AddUserHiddenChannelHandler : IRequestHandler<AddUserHiddenChannelRequest, ServiceResponse>
    {
        private readonly AccordContext _accordContext;
        private readonly IMediator _mediator;

        public AddUserHiddenChannelHandler(AccordContext accordContext, IMediator mediator)
        {
            _accordContext = accordContext;
            _mediator = mediator;
        }

        public async Task<ServiceResponse> Handle(AddUserHiddenChannelRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

            if (!userExists)
                return ServiceResponse.Fail("User does not exist");


            var userChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(request.DiscordUserId));

            if (userChannels.Contains(request.DiscordChannelId))
            {
                return ServiceResponse.Fail("This channel is already hidden for you.");
            }

            var userHiddenChannel = new UserHiddenChannel
            {
                UserId = request.DiscordUserId,
                DiscordChannelId = request.DiscordChannelId,
            };

            _accordContext.Add(userHiddenChannel);

            await _accordContext.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
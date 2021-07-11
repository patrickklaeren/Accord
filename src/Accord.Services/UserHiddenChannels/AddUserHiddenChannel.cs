using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHiddenChannels
{
    public sealed record AddUserHiddenChannelRequest(ulong DiscordUserId, ulong DiscordChannelId) : IRequest<ServiceResponse>;

    public sealed record AddUserHiddenChannelsRequest(ulong DiscordUserId, ulong DiscordChannelId, IReadOnlyList<ulong> DependentDiscordChannelIds) : IRequest<ServiceResponse>;

    public class AddUserHiddenChannelHandler :
        IRequestHandler<AddUserHiddenChannelRequest, ServiceResponse>,
        IRequestHandler<AddUserHiddenChannelsRequest, ServiceResponse>
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


            var userActiveHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);


            if (userActiveHiddenChannels.Any(channel => channel.DiscordChannelId == request.DiscordChannelId))
            {
                return ServiceResponse.Fail("This channel is already hidden for you.");
            }

            var userHiddenChannel = new UserHiddenChannel
            {
                UserId = request.DiscordUserId,
                DiscordChannelId = request.DiscordChannelId,
            };

            _accordContext.UserHiddenChannels.Add(userHiddenChannel);

            await _accordContext.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

            return ServiceResponse.Ok();
        }

        public async Task<ServiceResponse> Handle(AddUserHiddenChannelsRequest request, CancellationToken cancellationToken)
        {
            var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

            if (!userExists)
                return ServiceResponse.Fail("User does not exist");


            var userActiveHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

            if (userActiveHiddenChannels.Any(x => x.DiscordChannelId == request.DiscordChannelId))
            {
                return ServiceResponse.Fail("This channel is already hidden for you.");
            }

            var userHiddenChannel = new UserHiddenChannel
            {
                UserId = request.DiscordUserId,
                DiscordChannelId = request.DiscordChannelId,
            };

            _accordContext.UserHiddenChannels.Add(userHiddenChannel);

            foreach (var channel in request.DependentDiscordChannelIds)
            {
                if (userActiveHiddenChannels.All(x => x.DiscordChannelId != channel))
                {
                    _accordContext.UserHiddenChannels.Add(new UserHiddenChannel
                    {
                        UserId = request.DiscordUserId,
                        ParentDiscordChannelId = request.DiscordChannelId,
                        DiscordChannelId = channel,
                        CreatedAt = DateTimeOffset.Now
                    });
                }
                else
                {
                    var existingChannel = await _accordContext.UserHiddenChannels.SingleAsync(x => x.UserId == request.DiscordUserId && x.DiscordChannelId == channel,
                        cancellationToken);
                    existingChannel.ParentDiscordChannelId = request.DiscordChannelId;
                }
            }

            await _accordContext.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new InvalidateGetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

            return ServiceResponse.Ok();
        }
    }
}
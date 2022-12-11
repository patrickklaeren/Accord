using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserHiddenChannels;

public sealed record DeleteUserHiddenChannelRequest(ulong DiscordUserId, ulong DiscordChannelId, List<ulong>? DependentDiscordChannelIds = default) : IRequest<ServiceResponse>;

[AutoConstructor]
public partial class DeleteUserHiddenChannelHandler : IRequestHandler<DeleteUserHiddenChannelRequest, ServiceResponse>
{
    private readonly IMediator _mediator;
    private readonly AccordContext _accordContext;

    public async Task<ServiceResponse> Handle(DeleteUserHiddenChannelRequest request, CancellationToken cancellationToken)
    {
        var userExists = await _mediator.Send(new UserExistsRequest(request.DiscordUserId), cancellationToken);

        if (!userExists)
            return ServiceResponse.Fail("User does not exist");

        var userHiddenChannels = await _mediator.Send(new GetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

        UserHiddenChannel? hiddenChannel = null;

        if (userHiddenChannels.Any(x => x.DiscordChannelId == request.DiscordChannelId))
        {
            hiddenChannel = userHiddenChannels
                .Single(x => x.DiscordChannelId == request.DiscordChannelId);
            _accordContext.Remove(hiddenChannel);
        }

        var inheritedChannels = userHiddenChannels
            .Where(x => hiddenChannel != null
                        && x.ParentDiscordChannelId == hiddenChannel.DiscordChannelId
                        || request.DependentDiscordChannelIds != null
                        && request.DependentDiscordChannelIds.Contains(x.DiscordChannelId))
            .ToList();

        if (inheritedChannels.Any())
            _accordContext.RemoveRange(inheritedChannels);
            
        await _mediator.Send(new InvalidateGetUserHiddenChannelsRequest(request.DiscordUserId), cancellationToken);

        await _accordContext.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Starboard;

public sealed record AddStarboardChannelRequest(ulong? DiscordChannelBeingStarredInId, ulong DiscordStarboardChannelId) : IRequest<ServiceResponse>;

internal class AddStarboardChannelHandler(AccordContext db) 
    : IRequestHandler<AddStarboardChannelRequest, ServiceResponse>
{
    public async Task<ServiceResponse> Handle(AddStarboardChannelRequest request, CancellationToken cancellationToken)
    {
        var hasExistingChannel = await db.StarboardChannels
            .Where(x => x.DiscordChannelBeingStarredInId == null 
                        || x.DiscordChannelBeingStarredInId == request.DiscordChannelBeingStarredInId)
            .Where(x => x.DiscordStarboardChannelId == request.DiscordStarboardChannelId)
            .AnyAsync(cancellationToken: cancellationToken);

        if (hasExistingChannel)
        {
            return ServiceResponse.Fail("An existing starboard configuration for this already exists");
        }

        var channel = new StarboardChannel
        {
            DiscordStarboardChannelId = request.DiscordStarboardChannelId,
            DiscordChannelBeingStarredInId = request.DiscordChannelBeingStarredInId
        };

        db.StarboardChannels.Add(channel);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResponse.Ok();
    }
}
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using Accord.Services.ChannelFlags;
using Accord.Services.Starboard;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserMessages;

public sealed record UpdateMessageRequest(ulong DiscordMessageId, ulong DiscordUserId, string? NewContent) : IRequest, IEnsureUserExistsRequest;

internal class UpdateMessageHandler(AccordContext db,
    ChannelFlagService channelFlagService,
    StarboardService starboardService,
    IMediator mediator) : IRequestHandler<UpdateMessageRequest>
{
    public async Task Handle(UpdateMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await db.UserMessages
            .Where(x => x.Id == request.DiscordMessageId)
            .Where(x => x.UserId == request.DiscordUserId)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken);

        if (message is null)
            return;

        var oldContent = message.Content;
        message.Content = request.NewContent;

        await db.SaveChangesAsync(cancellationToken);

        if (!await channelFlagService.ChannelHasFlag(message.DiscordChannelId, ChannelFlagType.DoNotLogMessages, cancellationToken))
        {
            await mediator.Publish(new RelayMessageUpdateToDiscord(request.DiscordMessageId,
                    message.DiscordChannelId,
                    request.DiscordUserId,
                    oldContent,
                    request.NewContent),
                cancellationToken);
        }
        
        await starboardService.MessageUpdateForStarboard(request.DiscordUserId, request.DiscordMessageId, request.DiscordUserId, cancellationToken);
    }
}
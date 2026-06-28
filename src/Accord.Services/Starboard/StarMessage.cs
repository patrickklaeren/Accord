using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.Starboard;

public sealed record StarMessageRequest(
    ulong DiscordMessageId,
    ulong DiscordMessageChannelId,
    ulong? DiscordMessageChannelParentId,
    ulong DiscordMessageUserId) : IRequest;

internal class StarMessageHandler(StarboardService starboardService)
    : IRequestHandler<StarMessageRequest>
{
    public async Task Handle(StarMessageRequest request, CancellationToken cancellationToken)
    {
        await starboardService.HandleMessageForStarboard(request.DiscordMessageId,
            request.DiscordMessageChannelId,
            request.DiscordMessageChannelParentId,
            request.DiscordMessageUserId,
            cancellationToken);
    }
}
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.Moderation;
using Accord.Services.Users;
using MediatR;

namespace Accord.Services.UserMessages;

public sealed record IsCryptoSpamMessageRequest(ulong DiscordGuildId, ulong DiscordUserId, ICollection<string> FileUrls) : IRequest, IEnsureUserExistsRequest;

public class IsCryptoSpamMessageHandler(IMediator mediator, HttpClient httpClient) : IRequestHandler<IsCryptoSpamMessageRequest>
{
    private readonly ulong[] _knownHashes =
    [
        8517004236877842929,
        14106368200197518167,
        10368240047807509339,
        9774093885703008723
    ];

    public async Task Handle(IsCryptoSpamMessageRequest request, CancellationToken cancellationToken)
    {
        foreach (var fileUrl in request.FileUrls)
        {
            await using var stream = await httpClient.GetStreamAsync(fileUrl, cancellationToken);
            var hashOfImage = ImageSimilarityService.ComputeDHash(stream);
            
            foreach (var knownHash in _knownHashes)
            {
                var distance = ImageSimilarityService.Distance(hashOfImage, knownHash);

                const int SIMILAR_FACTOR = 20;
                
                if (distance > SIMILAR_FACTOR)
                {
                    continue;
                }

                // probably same image
                var user = await mediator.Send(new GetUserRequest(request.DiscordUserId), cancellationToken);

                if (user.Failure)
                {
                    // Bail fast we don't have a user
                    // Fallback and delete the message
                }

                var userToKick = user.Value!.User;

                await mediator.Send(new KickRequest(request.DiscordGuildId,
                        request.DiscordUserId,
                        userToKick.Username ?? userToKick.Id.ToString(), 
                        "Crypto scam detected"),
                    cancellationToken);
            }
        }
    }
}
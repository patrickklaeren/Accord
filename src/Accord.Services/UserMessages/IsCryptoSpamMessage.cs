using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.UserMessages;

public sealed record IsCryptoSpamMessageRequest(ulong DiscordGuildId, ulong DiscordUserId, ICollection<string> FileUrls) : IRequest, IEnsureUserExistsRequest;

public sealed record CryptoScamAlertRequest(ulong DiscordUserId, string FileUrl) : IRequest;

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

                const int SIMILAR_FACTOR = 15;
                
                if (distance > SIMILAR_FACTOR)
                {
                    continue;
                }

                await mediator.Send(new CryptoScamAlertRequest(request.DiscordUserId, fileUrl), cancellationToken);
            }
        }
    }
}
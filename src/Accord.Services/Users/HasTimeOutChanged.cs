using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record HasTimeOutChangedRequest(ulong DiscordUserId, DateTimeOffset? Candidate) : IRequest<bool>;

public class HasTimeOutChangedHandler(AccordContext db, IAppCache appCache) : IRequestHandler<HasTimeOutChangedRequest, bool>
{

    public async Task<bool> Handle(HasTimeOutChangedRequest request, CancellationToken cancellationToken)
    {
        var hasTimeOutRecorded = await appCache
            .GetOrAddAsync(BuildCacheKey(request.DiscordUserId),
                () => db.Users.AnyAsync(x => x.Id == request.DiscordUserId && x.TimedOutUntil == request.Candidate, cancellationToken),
                DateTimeOffset.UtcNow.AddMinutes(5));

        return !hasTimeOutRecorded;
    }

    internal static string BuildCacheKey(ulong discordUserId)
    {
        return $"{nameof(HasTimeOutChangedHandler)}/{nameof(HasTimeOutChangedRequest)}/{discordUserId}";
    }
}
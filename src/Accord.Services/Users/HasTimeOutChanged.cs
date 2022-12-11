using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.Users;

public sealed record HasTimeOutChangedRequest(ulong DiscordUserId, DateTimeOffset? Candidate) : IRequest<bool>;

[AutoConstructor]
public partial class HasTimeOutChangedHandler : IRequestHandler<HasTimeOutChangedRequest, bool>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<bool> Handle(HasTimeOutChangedRequest request, CancellationToken cancellationToken)
    {
        var hasTimeOutRecorded = await _appCache
            .GetOrAddAsync(BuildCacheKey(request.DiscordUserId),
                () => _db.Users.AnyAsync(x => x.Id == request.DiscordUserId && x.TimedOutUntil == request.Candidate, cancellationToken),
                DateTimeOffset.Now.AddMinutes(5));

        return !hasTimeOutRecorded;
    }

    internal static string BuildCacheKey(ulong discordUserId)
    {
        return $"{nameof(HasTimeOutChangedHandler)}/{nameof(HasTimeOutChangedRequest)}/{discordUserId}";
    }
}
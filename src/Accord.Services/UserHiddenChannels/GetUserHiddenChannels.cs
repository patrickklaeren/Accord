using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using Accord.Domain.Model;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserHiddenChannels;

public sealed record GetUserHiddenChannelsRequest(ulong DiscordUserId) : IRequest<List<UserHiddenChannel>>;

public sealed record InvalidateGetUserHiddenChannelsRequest(ulong DiscordUserId) : IRequest;

[AutoConstructor]
public partial class GetUserHiddenChannelsHandler :
    IRequestHandler<InvalidateGetUserHiddenChannelsRequest>,
    IRequestHandler<GetUserHiddenChannelsRequest, List<UserHiddenChannel>>
{
    private readonly AccordContext _db;
    private readonly IAppCache _appCache;

    public async Task<List<UserHiddenChannel>> Handle(GetUserHiddenChannelsRequest request, CancellationToken cancellationToken) =>
        await _appCache.GetOrAddAsync(
            BuildGetUserHiddenChannelsById(request.DiscordUserId),
            () => GetUserHiddenChannelsById(request.DiscordUserId),
            DateTimeOffset.Now.AddDays(30)
        );

    private async Task<List<UserHiddenChannel>> GetUserHiddenChannelsById(ulong userId) =>
        await _db.UserHiddenChannels
            .Where(x => x.UserId == userId)
            .ToListAsync();

    private static string BuildGetUserHiddenChannelsById(ulong discordUserId) =>
        $"{nameof(GetUserHiddenChannelsHandler)}/{nameof(GetUserHiddenChannelsById)}/{discordUserId}";

    public Task Handle(InvalidateGetUserHiddenChannelsRequest request, CancellationToken cancellationToken)
    {
        _appCache.Remove(BuildGetUserHiddenChannelsById(request.DiscordUserId));
        return Task.CompletedTask;
    }
}
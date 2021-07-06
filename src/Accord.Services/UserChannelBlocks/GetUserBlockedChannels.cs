using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserChannelBlocks
{
    public sealed record GetUserBlockedChannelsRequest(ulong DiscordUserId) : IRequest<List<ulong>>;

    public sealed record InvalidateGetUserBlockedChannelsRequest(ulong DiscordUserId) : IRequest;

    public class GetUserBlockedChannelsHandler :
        RequestHandler<InvalidateGetUserBlockedChannelsRequest>,
        IRequestHandler<GetUserBlockedChannelsRequest, List<ulong>>
    {
        private readonly AccordContext _db;
        private readonly IAppCache _appCache;


        public GetUserBlockedChannelsHandler(AccordContext db, IAppCache appCache)
        {
            _db = db;
            _appCache = appCache;
        }

        public async Task<List<ulong>> Handle(GetUserBlockedChannelsRequest request, CancellationToken cancellationToken) =>
            await _appCache.GetOrAddAsync(
                BuildGetUserBlockedChannelsById(request.DiscordUserId),
                () => GetUserBlockedChannelsById(request.DiscordUserId),
                DateTimeOffset.Now.AddDays(30)
            );

        protected override void Handle(InvalidateGetUserBlockedChannelsRequest request) =>
            _appCache.Remove(BuildGetUserBlockedChannelsById(request.DiscordUserId));

        private async Task<List<ulong>> GetUserBlockedChannelsById(ulong userId) =>
            await _db.UserBlockedChannels
                .Where(x => x.UserId == userId)
                .Select(x => x.DiscordChannelId)
                .ToListAsync();

        private static string BuildGetUserBlockedChannelsById(ulong discordUserId) =>
            $"{nameof(GetUserBlockedChannelsHandler)}/{nameof(GetUserBlockedChannelsById)}/{discordUserId}";
    }
}
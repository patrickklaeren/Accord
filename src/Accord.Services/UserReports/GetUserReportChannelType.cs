using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain;
using LazyCache;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accord.Services.UserReports;

public sealed record GetUserReportChannelTypeRequest(ulong DiscordChannelId) : IRequest<UserReportChannelType>;

public class GetUserReportChannelTypeHandler(AccordContext db, IAppCache appCache) : IRequestHandler<GetUserReportChannelTypeRequest, UserReportChannelType>
{

    public async Task<UserReportChannelType> Handle(GetUserReportChannelTypeRequest channelTypeRequest, CancellationToken cancellationToken)
    {
        return await appCache.GetOrAddAsync(BuildIsUserReportChannelCacheKey(channelTypeRequest.DiscordChannelId),
            () => IsUserReportChannel(channelTypeRequest.DiscordChannelId),
            DateTimeOffset.UtcNow.AddDays(30));
    }

    private static string BuildIsUserReportChannelCacheKey(ulong discordChannelId)
    {
        return $"{nameof(GetUserReportChannelTypeHandler)}/{nameof(IsUserReportChannel)}/{discordChannelId}";
    }

    private async Task<UserReportChannelType> IsUserReportChannel(ulong discordChannelId)
    {
        var userReport = await db.UserReports
            .Where(x => x.OutboxDiscordChannelId == discordChannelId
                        || x.InboxDiscordChannelId == discordChannelId)
            .Select(x => new
            {
                x.OutboxDiscordChannelId,
                x.InboxDiscordChannelId
            }).FirstOrDefaultAsync();

        if (userReport is null)
            return UserReportChannelType.None;

        if (userReport.InboxDiscordChannelId == discordChannelId)
            return UserReportChannelType.Inbox;

        if (userReport.OutboxDiscordChannelId == discordChannelId)
            return UserReportChannelType.Outbox;

        return UserReportChannelType.None;
    }
}